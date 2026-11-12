using System.Diagnostics;
using StudioManager.Application;
using Xunit;

namespace StudioManager.Tests;

[CollectionDefinition("Process diagnostic listeners", DisableParallelization = true)]
public sealed class DiagnosticListenerCollection { }

[Collection("Process diagnostic listeners")]
public sealed class DiagnosticListenerFailureTests
{
    [Fact]
    public void FailingDiagnosticListener_CannotReplaceSafePublicError()
    {
        using var listener = new FailingListener();
        Trace.Listeners.Add(listener);
        try
        {
            var report = SafeDiagnostics.Capture(new IOException("synthetic private path"), "Acceptance.Diagnostics");
            Assert.Equal("UNEXPECTED_ERROR", report.Code);
            Assert.DoesNotContain("synthetic private path", report.Message + report.DiagnosticJson);
            Assert.Contains(report.CorrelationId, report.Message);
            Assert.True(listener.WriteCalls > 0);
        }
        finally { Trace.Listeners.Remove(listener); }
    }

    private sealed class FailingListener : TraceListener
    {
        public int WriteCalls { get; private set; }
        public override void Write(string? message) { WriteCalls++; throw new InvalidOperationException("Synthetic sink failure"); }
        public override void WriteLine(string? message) { WriteCalls++; throw new InvalidOperationException("Synthetic sink failure"); }
    }
}
