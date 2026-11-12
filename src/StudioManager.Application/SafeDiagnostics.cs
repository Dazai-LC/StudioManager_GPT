using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace StudioManager.Application;

/// <summary>A public error and its deliberately minimal, non-sensitive diagnostic event.</summary>
public sealed record SafeError(string Code, string Message, string CorrelationId, string DiagnosticJson);

public static class SafeDiagnostics
{
    private static readonly ConditionalWeakTable<Exception, SafeError> Reports = new();

    /// <summary>
    /// Never serializes exception messages, stack traces, Data, SQL commands, account data or paths.
    /// Reusing an exception produces the same correlation ID across Application and UI boundaries.
    /// </summary>
    public static SafeError Capture(Exception exception, string stage, int? providerCode = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return Reports.GetValue(exception, error => Create(error, stage, providerCode));
    }

    private static SafeError Create(Exception exception, string stage, int? providerCode)
    {
        var databaseError = false;
        for (var current = exception; current is not null; current = current.InnerException)
            databaseError |= string.Equals(current.GetType().Name, "SqlException", StringComparison.Ordinal);

        var correlationId = Guid.NewGuid().ToString("N");
        var code = exception is OperationCanceledException ? "CANCELLED" : databaseError ? "DB_ERROR" : "UNEXPECTED_ERROR";
        var description = code switch
        {
            "CANCELLED" => "Thao tác đã bị hủy. Vui lòng kiểm tra trạng thái trước khi thử lại.",
            "DB_ERROR" => "Không thể kết nối hoặc xử lý dữ liệu SQL Server. Vui lòng thử lại hoặc liên hệ quản trị viên.",
            _ => "Không thể hoàn tất thao tác. Vui lòng thử lại hoặc liên hệ quản trị viên."
        };
        var diagnosticJson = JsonSerializer.Serialize(new
        {
            Event = "ApplicationError",
            Code = code,
            CorrelationId = correlationId,
            TimestampUtc = DateTimeOffset.UtcNow,
            Stage = SafeStage(stage),
            ExceptionType = exception.GetType().FullName,
            InnerExceptionType = exception.InnerException?.GetType().FullName,
            ProviderCode = providerCode
        });
        var report = new SafeError(code, $"{description} Mã tra cứu: {correlationId}.", correlationId, diagnosticJson);
        try { Trace.TraceError(report.DiagnosticJson); }
        catch { /* Diagnostic listeners must not replace the safe public error. */ }
        return report;
    }

    // Stages are developer-owned identifiers, never user input or interpolated details.
    private static string SafeStage(string? stage)
        => !string.IsNullOrEmpty(stage) && stage.Length <= 96 &&
           char.IsAsciiLetter(stage[0]) &&
           stage.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_')
            ? stage
            : "Application";
}
