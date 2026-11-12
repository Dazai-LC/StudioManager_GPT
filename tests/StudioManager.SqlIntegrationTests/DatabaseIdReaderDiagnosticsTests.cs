using System.Collections;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Globalization;
using Xunit;

namespace StudioManager.SqlIntegrationTests;

public sealed class DatabaseIdReaderDiagnosticsTests
{
    private const string Secret = "private-db-value=37;Server=private-server;Password=private-password";
    private const string PrivatePath = @"C:\Private\Studio\reader-secret.sql";

    [Theory]
    [InlineData("closed", "READ_DATABASE_ID_READER_INVALID")]
    [InlineData("column_missing", "READ_DATABASE_ID_READER_INVALID")]
    [InlineData("state_probe", "READ_DATABASE_ID_STATE_CHECK_FAILED")]
    [InlineData("field_count_probe", "READ_DATABASE_ID_STATE_CHECK_FAILED")]
    [InlineData("null", "READ_DATABASE_ID_SQL_NULL")]
    [InlineData("null_probe", "READ_DATABASE_ID_NULL_CHECK_FAILED")]
    [InlineData("short_value", "READ_DATABASE_ID_FAILED")]
    [InlineData("long_value", "READ_DATABASE_ID_FAILED")]
    [InlineData("decimal_value", "READ_DATABASE_ID_FAILED")]
    [InlineData("int_value", "READ_DATABASE_ID_FAILED")]
    [InlineData("overflow_value", "READ_DATABASE_ID_FAILED")]
    [InlineData("getter", "READ_DATABASE_ID_FAILED")]
    [InlineData("null_text", "READ_DATABASE_ID_FAILED")]
    public void DatabaseIdFailure_IsPreciseRedactedAndDoesNotRunTheGetterPrematurely(string fault, string reason)
    {
        using var reader = new ProbeReader(fault);
        Assert.True(reader.Read());
        var error = Assert.Throws<SqlTestDiagnosticException>(() => SqlTargetIdentityReader.ReadDatabaseId(reader));
        AssertReason(error, reason);
        Assert.Equal(fault is "getter" or "null_text" || fault.EndsWith("_value", StringComparison.Ordinal) ? 1 : 0, reader.StringCalls);
        Assert.Equal(0, reader.Int32Calls);
        Assert.Equal(0, reader.ValueCalls);
        Assert.DoesNotContain("clr-type", reader.DatabaseIdCalls);
        Assert.DoesNotContain("provider-type", reader.DatabaseIdCalls);
        Assert.DoesNotContain("sql-type", reader.DatabaseIdCalls);
        if (fault == "getter")
            Assert.Equal(new[] { "state", "field-count", "null", "get-string" }, reader.DatabaseIdCalls);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("sql_uppercase")]
    [InlineData("clr_provider")]
    [InlineData("unexpected_clr")]
    [InlineData("unexpected_provider")]
    [InlineData("clr_probe")]
    [InlineData("provider_probe")]
    [InlineData("sql_smallint")]
    [InlineData("sql_bigint")]
    [InlineData("sql_nvarchar")]
    [InlineData("sql_clr_name")]
    [InlineData("sql_empty")]
    [InlineData("sql_null")]
    [InlineData("sql_padding")]
    [InlineData("sql_private")]
    [InlineData("sql_probe")]
    public void PositiveDatabaseIdText_IsParsed_WithoutConsultingMetadataHints(string metadata)
    {
        using var reader = new ProbeReader(metadata);
        Assert.True(reader.Read());
        Assert.Equal(37, SqlTargetIdentityReader.ReadDatabaseId(reader));
        Assert.Equal(1, reader.StringCalls);
        Assert.Equal(0, reader.Int32Calls);
        Assert.Equal(0, reader.ValueCalls);
        Assert.Equal(new[] { "state", "field-count", "null", "get-string" }, reader.DatabaseIdCalls);
    }

    [Theory]
    [InlineData("closed_mid_row", "READ_DATABASE_ID_READER_INVALID")]
    [InlineData("null", "READ_DATABASE_ID_SQL_NULL")]
    [InlineData("short_value", "READ_DATABASE_ID_FAILED")]
    [InlineData("long_value", "READ_DATABASE_ID_FAILED")]
    [InlineData("decimal_value", "READ_DATABASE_ID_FAILED")]
    [InlineData("int_value", "READ_DATABASE_ID_FAILED")]
    [InlineData("overflow_value", "READ_DATABASE_ID_FAILED")]
    [InlineData("getter", "READ_DATABASE_ID_FAILED")]
    public async Task ProductionRowAndStagePath_PreservesReasonAndStopsLaterChecks(string fault, string reason)
    {
        await using var reader = new ProbeReader(fault);
        var laterChecks = 0;
        var cleanupCalls = 0;
        Task LaterCheck() { laterChecks++; return Task.CompletedTask; }

        var error = await Assert.ThrowsAsync<SqlTestDiagnosticException>(() => SqlTestDiagnostics.VerifyConnectionAsync(
            () => Task.CompletedTask,
            async () => { await SqlTargetIdentityReader.ReadAsync(reader); },
            LaterCheck, LaterCheck, null,
            () => { cleanupCalls++; return reader.DisposeAsync(); }));

        AssertReason(error, reason);
        Assert.Equal(0, laterChecks);
        Assert.Equal(1, cleanupCalls);
        Assert.Equal(fault == "getter" || fault.EndsWith("_value", StringComparison.Ordinal) ? 1 : 0, reader.StringCalls);
        Assert.Equal(0, reader.Int32Calls);
        Assert.Equal(0, reader.ValueCalls);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("37", 37)]
    [InlineData("00037", 37)]
    [InlineData("2147483647", int.MaxValue)]
    public void PositiveDecimalText_IsParsedInvariantly_AndReturnsInt32(string text, int expected)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            using var reader = new ProbeReader("none", text);
            Assert.True(reader.Read());
            Assert.Equal(expected, SqlTargetIdentityReader.ReadDatabaseId(reader));
            Assert.Equal(1, reader.StringCalls);
            Assert.Equal(0, reader.Int32Calls);
            Assert.Equal(0, reader.ValueCalls);
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("000")]
    [InlineData("-1")]
    [InlineData("+1")]
    [InlineData("-2147483648")]
    [InlineData("2147483648")]
    [InlineData("99999999999")]
    [InlineData("abc")]
    [InlineData("37x")]
    [InlineData("37.0")]
    [InlineData("3,7")]
    [InlineData(" 37")]
    [InlineData("37 ")]
    [InlineData("\t37")]
    [InlineData("37\n")]
    [InlineData("\u0663\u0667")]
    [InlineData("\uFF13\uFF17")]
    [InlineData("37\0")]
    [InlineData("3\u00007")]
    [InlineData(Secret)]
    public void InvalidOrNonPositiveText_FailsClosed_WithoutFallbackOrDiagnosticValues(string text)
    {
        using var reader = new ProbeReader("none", text);
        Assert.True(reader.Read());
        AssertReason(Assert.Throws<SqlTestDiagnosticException>(() => SqlTargetIdentityReader.ReadDatabaseId(reader)), "READ_DATABASE_ID_FAILED");
        Assert.Equal(1, reader.StringCalls);
        Assert.Equal(0, reader.Int32Calls);
        Assert.Equal(0, reader.ValueCalls);
    }

    private static void AssertReason(SqlTestDiagnosticException error, string reason)
    {
        Assert.Equal(SqlTestDiagnosticStage.TARGET_IDENTITY, error.Stage);
        Assert.Equal(reason, error.Reason.ToString());
        Assert.Equal($"SQL test failed at stage TARGET_IDENTITY; reason {reason}. Diagnostic details are intentionally omitted.", error.Message);
        Assert.Null(error.InnerException);
        Assert.Null(error.StackTrace);
        Assert.Null(error.HelpLink);
        Assert.Empty(error.Data);
        var rendered = error.Message + error.ToString() + error.Source + error.GetBaseException();
        Assert.DoesNotContain(Secret, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("private-password", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(PrivatePath, rendered, StringComparison.Ordinal);
    }

    private static Exception PrivateFailure()
    {
        var error = new InvalidCastException(Secret, new IOException(PrivatePath)) { Source = PrivatePath, HelpLink = PrivatePath };
        error.Data["ConnectionString"] = Secret;
        return error;
    }

    // Only reader API calls are faulted; no connection or SQL command is constructed.
    internal sealed class ProbeReader : DbDataReader
    {
        private readonly string _fault;
        private readonly DataTable _table;
        private readonly DataTableReader _inner;
        internal List<string> DatabaseIdCalls { get; } = [];
        internal int Int32Calls { get; private set; }
        internal int StringCalls { get; private set; }
        internal int ValueCalls { get; private set; }

        internal ProbeReader(string fault) : this(CreateTable(fault), fault) { }

        internal ProbeReader(string fault, string databaseIdText) : this(CreateTable(fault), fault)
            => _table.Rows[0][7] = databaseIdText;

        internal ProbeReader(DataTable table, string fault = "none")
        {
            _fault = fault;
            _table = table;
            _inner = table.CreateDataReader();
        }

        private static DataTable CreateTable(string fault)
        {
            var table = new DataTable();
            object databaseId = fault switch
            {
                "null" => DBNull.Value,
                "short_value" => (short)37,
                "long_value" => 37L,
                "decimal_value" => 37m,
                "int_value" => 37,
                "overflow_value" => long.MaxValue,
                _ => "37"
            };
            string[] names = ["DatabaseName", "MachineName", "Updateability", "RunId", "SchemaVersion", "SourceChecksum",
                "InstanceName", "DatabaseId", "CreatedAt", "MetadataFingerprint", "ServerName"];
            for (var index = 0; index < names.Length; index++)
                table.Columns.Add(names[index], index == 7 ? databaseId is DBNull ? typeof(string) : databaseId.GetType() : index == 8 ? typeof(DateTime) : typeof(string));
            table.Rows.Add(Secret, Secret, "READ_WRITE", Secret, Secret, Secret, Secret,
                databaseId, new DateTime(2026, 11, 12), Secret, Secret);
            return table;
        }

        public override bool IsClosed
        {
            get
            {
                DatabaseIdCalls.Add("state");
                if (_fault == "state_probe") throw PrivateFailure();
                return _fault == "closed" || _inner.IsClosed;
            }
        }

        public override int FieldCount
        {
            get
            {
                DatabaseIdCalls.Add("field-count");
                if (_fault == "field_count_probe") throw PrivateFailure();
                return _fault == "column_missing" ? 7 : _inner.FieldCount;
            }
        }

        public override bool IsDBNull(int ordinal)
        {
            if (ordinal == 7)
            {
                DatabaseIdCalls.Add("null");
                if (_fault == "null_probe") throw PrivateFailure();
            }
            return _inner.IsDBNull(ordinal);
        }

        public override Type GetFieldType(int ordinal)
        {
            if (ordinal == 7)
            {
                DatabaseIdCalls.Add("clr-type");
                if (_fault == "clr_probe") throw PrivateFailure();
                if (_fault == "unexpected_clr") return typeof(long);
            }
            return _inner.GetFieldType(ordinal);
        }

        public override Type GetProviderSpecificFieldType(int ordinal)
        {
            if (ordinal != 7) return _inner.GetProviderSpecificFieldType(ordinal);
            DatabaseIdCalls.Add("provider-type");
            if (_fault == "provider_probe") throw PrivateFailure();
            return _fault == "unexpected_provider" ? typeof(SqlInt16) :
                _fault == "clr_provider" ? typeof(int) : typeof(SqlInt32);
        }

        public override int GetInt32(int ordinal)
        {
            if (ordinal == 7)
            {
                Int32Calls++;
                DatabaseIdCalls.Add("get-int32");
                if (_fault == "getter") throw PrivateFailure();
            }
            return _inner.GetInt32(ordinal);
        }

        public override string GetString(int ordinal)
        {
            if (ordinal == 7)
            {
                StringCalls++;
                DatabaseIdCalls.Add("get-string");
                if (_fault == "getter") throw PrivateFailure();
                if (_fault == "null_text") return null!;
            }
            var value = _inner.GetString(ordinal);
            if (ordinal == 6 && _fault == "closed_mid_row") _inner.Close();
            return value;
        }

        public override object GetValue(int ordinal) { ValueCalls++; throw PrivateFailure(); }
        public override int GetValues(object[] values) { ValueCalls++; throw PrivateFailure(); }
        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => GetValue(GetOrdinal(name));
        public override int Depth => _inner.Depth;
        public override bool HasRows => _inner.HasRows;
        public override int RecordsAffected => _inner.RecordsAffected;
        public override string GetName(int ordinal) => _inner.GetName(ordinal);
        public override int GetOrdinal(string name) => _inner.GetOrdinal(name);
        public override string GetDataTypeName(int ordinal)
        {
            if (ordinal != 7) return _inner.GetDataTypeName(ordinal);
            DatabaseIdCalls.Add("sql-type");
            if (_fault == "sql_probe") throw PrivateFailure();
            return _fault switch
            {
                "sql_smallint" => "smallint",
                "sql_bigint" => "bigint",
                "sql_nvarchar" => "nvarchar",
                "sql_clr_name" => "Int32",
                "sql_empty" => "",
                "sql_null" => null!,
                "sql_padding" => "int ",
                "sql_private" => Secret,
                "sql_uppercase" => "INT",
                _ => _inner.GetFieldType(ordinal) == typeof(int) ? "int" : _inner.GetDataTypeName(ordinal)
            };
        }
        public override bool Read() => _inner.Read();
        public override Task<bool> ReadAsync(CancellationToken token) => _inner.ReadAsync(token);
        public override bool NextResult() => _inner.NextResult();
        public override bool GetBoolean(int ordinal) => _inner.GetBoolean(ordinal);
        public override byte GetByte(int ordinal) => _inner.GetByte(ordinal);
        public override long GetBytes(int ordinal, long offset, byte[]? buffer, int bufferOffset, int length) => _inner.GetBytes(ordinal, offset, buffer, bufferOffset, length);
        public override char GetChar(int ordinal) => _inner.GetChar(ordinal);
        public override long GetChars(int ordinal, long offset, char[]? buffer, int bufferOffset, int length) => _inner.GetChars(ordinal, offset, buffer, bufferOffset, length);
        public override DateTime GetDateTime(int ordinal) => _inner.GetDateTime(ordinal);
        public override decimal GetDecimal(int ordinal) => _inner.GetDecimal(ordinal);
        public override double GetDouble(int ordinal) => _inner.GetDouble(ordinal);
        public override float GetFloat(int ordinal) => _inner.GetFloat(ordinal);
        public override Guid GetGuid(int ordinal) => _inner.GetGuid(ordinal);
        public override short GetInt16(int ordinal) => _inner.GetInt16(ordinal);
        public override long GetInt64(int ordinal) => _inner.GetInt64(ordinal);
        public override IEnumerator GetEnumerator() => ((IEnumerable)_inner).GetEnumerator();
        public override void Close() => _inner.Close();
        protected override void Dispose(bool disposing)
        {
            if (disposing) { _inner.Dispose(); _table.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
