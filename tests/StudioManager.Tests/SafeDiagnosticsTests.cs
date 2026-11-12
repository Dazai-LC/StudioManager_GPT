using System.Text.Json;
using StudioManager.Application;
using Xunit;

namespace StudioManager.Tests;

public sealed class SafeDiagnosticsTests
{
    [Fact]
    public void Capture_DoesNotExposeExceptionPayloadOrPrivatePaths()
    {
        const string secret = "Password=private-credential;PBKDF2-SHA256$210000$salt$hash";
        const string privatePath = @"C:\Private\Customers\album.jpg";
        var exception = new InvalidOperationException(secret, new IOException(privatePath));
        exception.Data["ConnectionString"] = secret;

        var report = SafeDiagnostics.Capture(exception, "Booking.Update");

        Assert.Equal("UNEXPECTED_ERROR", report.Code);
        Assert.DoesNotContain(secret, report.Message + report.DiagnosticJson);
        Assert.DoesNotContain(privatePath, report.Message + report.DiagnosticJson);
        Assert.DoesNotContain("private-credential", report.Message + report.DiagnosticJson);
        Assert.DoesNotContain("album.jpg", report.Message + report.DiagnosticJson);
        Assert.DoesNotContain("ConnectionString", report.DiagnosticJson);
        Assert.Contains(report.CorrelationId, report.Message);
        Assert.True(Guid.TryParseExact(report.CorrelationId, "N", out _));
        using var diagnostic = JsonDocument.Parse(report.DiagnosticJson);
        Assert.Equal("Booking.Update", diagnostic.RootElement.GetProperty("Stage").GetString());
        Assert.Equal(typeof(InvalidOperationException).FullName, diagnostic.RootElement.GetProperty("ExceptionType").GetString());
        Assert.False(diagnostic.RootElement.TryGetProperty("StackTrace", out _));
    }

    [Fact]
    public void Capture_SameFailureKeepsOneCorrelationAcrossBoundaries()
    {
        var exception = new InvalidOperationException("private details");

        var repositoryReport = SafeDiagnostics.Capture(exception, "Repository.Save", 1205);
        var uiReport = SafeDiagnostics.Capture(exception, "LoginForm.LoginAsync");

        Assert.Same(repositoryReport, uiReport);
        using var diagnostic = JsonDocument.Parse(uiReport.DiagnosticJson);
        Assert.Equal(1205, diagnostic.RootElement.GetProperty("ProviderCode").GetInt32());
    }

    [Theory]
    [InlineData(@"C:\Private\Customers")]
    [InlineData("Account.Password=secret")]
    [InlineData("Login\r\nInjected=true")]
    public void Capture_RejectsUnstructuredDiagnosticStage(string unsafeStage)
    {
        var report = SafeDiagnostics.Capture(new Exception("hidden"), unsafeStage);

        using var diagnostic = JsonDocument.Parse(report.DiagnosticJson);
        Assert.Equal("Application", diagnostic.RootElement.GetProperty("Stage").GetString());
        Assert.DoesNotContain(unsafeStage, report.DiagnosticJson);
    }

    [Fact]
    public void Capture_CancellationUsesSafeStableCode()
    {
        var report = SafeDiagnostics.Capture(new OperationCanceledException("private query"), "Repository.Save");

        Assert.Equal("CANCELLED", report.Code);
        Assert.DoesNotContain("private query", report.Message + report.DiagnosticJson);
    }
}
