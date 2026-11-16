using System.Text.Json;
using Microsoft.Data.SqlClient;
using StudioManager.Application;
using StudioManager.Domain;

namespace StudioManager.Infrastructure;

public sealed partial class SqlStudioRepository
{
    // Caller owns commit/rollback. Both the existing internal writer and P04 approval use this kernel.
    internal static async Task<long> CreateBookingWithinTransactionAsync(SqlConnection cn, SqlTransaction tx,
        BookingInput input, UserSession user, BookingQuote? agreedRequest, CancellationToken ct)
    {
        user = await RequireActorAsync(cn, tx, user, false, ct);
        await ValidateBookingAllocationAsync(cn, tx, input, ct);
        string name; decimal price;
        await using (var package = new SqlCommand("SELECT TenGoiChup,GiaGoi FROM dbo.GoiChup WITH(UPDLOCK,HOLDLOCK) WHERE GoiChupId=@G AND TrangThai='DANG_AP_DUNG'", cn, tx))
        {
            package.Parameters.AddWithValue("@G", input.GoiChupId);
            await using var r = await package.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) throw new BookingRequestRuleException("INACTIVE_PACKAGE");
            name = agreedRequest?.Package.Name ?? r.GetString(0);
            price = agreedRequest?.Package.Price ?? r.GetDecimal(1);
        }
        if (agreedRequest is not null)
        {
            var settlement = BookingMoneyPolicy.QuoteTotal(price, agreedRequest.Addons);
            if (!settlement.Success) throw new BookingRequestRuleException(settlement.Code);
            if (agreedRequest.Package.Id != input.GoiChupId || agreedRequest.Estimate != settlement.Data)
                throw new BookingRequestRuleException("STALE_REQUEST");
        }
        var code = $"LC{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}"[..25];
        await using var command = new SqlCommand("""
            INSERT dbo.LichChup(MaLichChup,KhachHangId,GoiChupId,TenGoiChot,GiaGoiChot,BatDau,KetThuc,
              NhiepAnhGiaId,PhongChupId,TrangThai,TienGiam,GhiChu,CreatedBy,CreatedAt,UpdatedBy,UpdatedAt)
            VALUES(@Code,@K,@G,@Name,@Price,@Start,@End,@N,@P,'DA_DAT_LICH',0,@Note,@User,GETDATE(),@User,GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """, cn, tx);
        command.Parameters.AddWithValue("@Code", code); command.Parameters.AddWithValue("@K", input.KhachHangId);
        command.Parameters.AddWithValue("@G", input.GoiChupId); command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@Price", price); command.Parameters.AddWithValue("@Start", input.BatDau);
        command.Parameters.AddWithValue("@End", input.KetThuc); command.Parameters.AddWithValue("@N", input.NhiepAnhGiaId);
        command.Parameters.AddWithValue("@P", input.PhongChupId); command.Parameters.AddWithValue("@Note", (object?)input.GhiChu ?? DBNull.Value);
        command.Parameters.AddWithValue("@User", user.TaiKhoanId);
        var id = Convert.ToInt64(await command.ExecuteScalarAsync(ct));
        foreach (var addon in agreedRequest?.Addons ?? [])
        {
            await using var add = new SqlCommand("""
                INSERT dbo.LichChupDichVu(LichChupId,DichVuId,TenDichVuChot,DonViTinhChot,DonGiaChot,SoLuong,CreatedBy,CreatedAt,UpdatedBy,UpdatedAt)
                SELECT @L,@D,@Name,@Unit,@Price,@Qty,@U,GETDATE(),@U,GETDATE()
                WHERE EXISTS(SELECT 1 FROM dbo.DichVu d WITH(UPDLOCK,HOLDLOCK) WHERE d.DichVuId=@D AND d.TrangThai='DANG_CUNG_CAP'
                  AND NOT EXISTS(SELECT 1 FROM dbo.TaiNguyen t WITH(HOLDLOCK) WHERE t.DichVuThueId=d.DichVuId));
                """, cn, tx);
            add.Parameters.AddWithValue("@L", id); add.Parameters.AddWithValue("@D", addon.ServiceId);
            add.Parameters.AddWithValue("@Name", addon.Name); add.Parameters.AddWithValue("@Unit", addon.Unit);
            add.Parameters.AddWithValue("@Price", addon.UnitPrice); add.Parameters.AddWithValue("@Qty", addon.Quantity);
            add.Parameters.AddWithValue("@U", user.TaiKhoanId);
            if (await add.ExecuteNonQueryAsync(ct) != 1) throw new BookingRequestRuleException("INACTIVE_SERVICE");
        }
        await AuditAsync(cn, tx, user, "TAO_LICH", "LichChup", id.ToString(), null, JsonSerializer.Serialize(input), null, ct);
        return id;
    }

    internal static async Task ValidateBookingAllocationAsync(SqlConnection cn, SqlTransaction tx, BookingInput input, CancellationToken ct)
    {
        if (!BusinessRules.KhoangThoiGianHopLe(input.BatDau, input.KetThuc)) throw new BookingRequestRuleException("INVALID_TIME");
        try { await RequireBookingAllocationAsync(cn, tx, input, ct); }
        catch (InvalidOperationException e) when (e.Message == "ALLOCATION_UNAVAILABLE")
        { throw new BookingRequestRuleException("ALLOCATION_UNAVAILABLE"); }
        // Existing room/photographer rows are transaction-locked above. Separate indexed ranges avoid a global schedule lock.
        foreach (var resource in new[] { (Column: "PhongChupId", Id: input.PhongChupId), (Column: "NhiepAnhGiaId", Id: input.NhiepAnhGiaId) })
        {
            await using var conflict = new SqlCommand("SELECT COUNT(*) FROM dbo.LichChup WITH(UPDLOCK,HOLDLOCK) WHERE " +
                resource.Column + "=@Resource AND TrangThai<>'DA_HUY' AND BatDau<@End AND KetThuc>@Start", cn, tx);
            conflict.Parameters.AddWithValue("@Resource", resource.Id);
            conflict.Parameters.AddWithValue("@Start", input.BatDau); conflict.Parameters.AddWithValue("@End", input.KetThuc);
            if (Convert.ToInt32(await conflict.ExecuteScalarAsync(ct)) > 0) throw new BookingRequestRuleException("CONFLICT");
        }
    }
}
