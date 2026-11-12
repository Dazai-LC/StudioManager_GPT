namespace StudioManager.SqlIntegrationTests;

internal enum SqlTestDiagnosticStage
{
    CONNECTION_OPEN,
    TARGET_IDENTITY,
    PERMISSIONS,
    SCHEMA_FINGERPRINT,
    SCHEMA_SHAPE
}

// Only TARGET_IDENTITY uses these fixed reasons. No observed values or exception text are included.
internal enum SqlTargetIdentityReason
{
    UNEXPECTED_FAILURE,
    CONFIG_MODE_INVALID, CONFIG_RUN_ID_INVALID, CONFIG_CONNECTION_MISSING, CONFIG_CONNECTION_INVALID,
    CONFIG_DATABASE_MISMATCH, CONFIG_AUTHENTICATION_REFUSED, CONFIG_CONNECTION_CAPABILITY_REFUSED,
    CONFIG_DATA_SOURCE_MISMATCH, WRITE_MODE_REQUIRED,
    QUERY_EXECUTION_FAILED, READER_READ_FAILED, READER_EMPTY, READER_LAYOUT_INVALID,
    READ_DATABASE_FAILED, READ_MACHINE_FAILED, READ_UPDATEABILITY_FAILED, READ_RUN_ID_FAILED,
    READ_VERSION_FAILED, READ_CHECKSUM_FAILED, READ_INSTANCE_FAILED, READ_DATABASE_ID_FAILED,
    READ_CREATED_AT_FAILED, READ_METADATA_FINGERPRINT_FAILED, READ_SERVER_FAILED,
    DATABASE_MISMATCH, MACHINE_MISMATCH, LOCAL_MACHINE_READ_FAILED, LOCAL_MACHINE_MISMATCH,
    INSTANCE_MISMATCH, SERVER_MISMATCH, RUN_ID_MISMATCH, VERSION_MISMATCH, CHECKSUM_MISMATCH,
    DATABASE_NOT_WRITABLE, DATABASE_ID_CHANGED, DATABASE_CREATED_AT_CHANGED,
    MANIFEST_ROOT_LOOKUP_FAILED, MANIFEST_ROOT_UNAVAILABLE, MANIFEST_SCRIPT_READ_FAILED,
    MANIFEST_ACCESS_DENIED, MANIFEST_SCRIPT_NOT_FOUND, MANIFEST_HASH_FAILED,
    READER_DISPOSE_FAILED, COMMAND_DISPOSE_FAILED,
    READ_DATABASE_ID_READER_INVALID, READ_DATABASE_ID_STATE_CHECK_FAILED,
    READ_DATABASE_ID_SQL_NULL, READ_DATABASE_ID_NULL_CHECK_FAILED,
    READ_DATABASE_ID_TYPE_UNEXPECTED, READ_DATABASE_ID_TYPE_CHECK_FAILED,
    READ_DATABASE_ID_GET_INT32_FAILED
}

/// <summary>Contains only a fixed stage; never retains the original exception or its payload.</summary>
internal sealed class SqlTestDiagnosticException : InvalidOperationException
{
    internal SqlTestDiagnosticException(SqlTestDiagnosticStage stage, SqlTargetIdentityReason? reason = null)
        : base(MessageFor(stage, reason))
    {
        Stage = stage;
        Reason = stage == SqlTestDiagnosticStage.TARGET_IDENTITY ? reason ?? SqlTargetIdentityReason.UNEXPECTED_FAILURE : null;
    }

    internal SqlTestDiagnosticStage Stage { get; }
    internal SqlTargetIdentityReason? Reason { get; }

    // Runners may print StackTrace separately from ToString. Neither surface may disclose local paths.
    public override string? StackTrace => null;
    public override string ToString() => $"{nameof(SqlTestDiagnosticException)}: {Message}";

    private static string MessageFor(SqlTestDiagnosticStage stage, SqlTargetIdentityReason? reason)
    {
        var identifier = stage switch
        {
            SqlTestDiagnosticStage.CONNECTION_OPEN => "CONNECTION_OPEN",
            SqlTestDiagnosticStage.TARGET_IDENTITY => "TARGET_IDENTITY",
            SqlTestDiagnosticStage.PERMISSIONS => "PERMISSIONS",
            SqlTestDiagnosticStage.SCHEMA_FINGERPRINT => "SCHEMA_FINGERPRINT",
            SqlTestDiagnosticStage.SCHEMA_SHAPE => "SCHEMA_SHAPE",
            _ => throw new InvalidOperationException("SQL test diagnostic stage refused.")
        };
        if (reason is { } supplied && (stage != SqlTestDiagnosticStage.TARGET_IDENTITY || !Enum.IsDefined(supplied)))
            throw new InvalidOperationException("SQL test diagnostic reason refused.");
        var detail = stage == SqlTestDiagnosticStage.TARGET_IDENTITY
            ? $"; reason {reason ?? SqlTargetIdentityReason.UNEXPECTED_FAILURE}" : "";
        return $"SQL test failed at stage {identifier}{detail}. Diagnostic details are intentionally omitted.";
    }
}

internal static class SqlTestDiagnostics
{
    internal static T TargetIdentity<T>(Func<T> validate)
        => TargetIdentity(SqlTargetIdentityReason.UNEXPECTED_FAILURE, validate);

    internal static T TargetIdentity<T>(SqlTargetIdentityReason reason, Func<T> validate)
    {
        try { return validate(); }
        catch (Exception error) { throw Sanitize(SqlTestDiagnosticStage.TARGET_IDENTITY, error, reason); }
    }

    internal static async Task<T> TargetIdentityAsync<T>(SqlTargetIdentityReason reason, Func<Task<T>> operation)
    {
        try { return await operation(); }
        catch (Exception error) { throw Sanitize(SqlTestDiagnosticStage.TARGET_IDENTITY, error, reason); }
    }

    internal static async Task<T> TargetResourceAsync<TResource, T>(SqlTargetIdentityReason acquireReason,
        SqlTargetIdentityReason disposeReason, Func<Task<TResource>> acquire, Func<TResource, Task<T>> use)
        where TResource : IAsyncDisposable
    {
        var resource = await TargetIdentityAsync(acquireReason, acquire);
        T result;
        try { result = await use(resource); }
        catch
        {
            try { await resource.DisposeAsync(); }
            catch { /* Preserve the primary failure if closing a reader/command also fails. */ }
            throw;
        }
        await TargetIdentityAsync(disposeReason, async () => { await resource.DisposeAsync(); return true; });
        return result;
    }

    internal static async Task RunAsync(SqlTestDiagnosticStage stage, Func<Task> operation)
    {
        try { await operation(); }
        catch (Exception error) { throw Sanitize(stage, error); }
    }

    private static SqlTestDiagnosticException Sanitize(SqlTestDiagnosticStage stage, Exception error,
        SqlTargetIdentityReason fallback = SqlTargetIdentityReason.UNEXPECTED_FAILURE)
    {
        // Rebuild even a known exception: never propagate its Data, Source, HelpLink or stack.
        var reason = stage == SqlTestDiagnosticStage.TARGET_IDENTITY
            ? error is SqlTestDiagnosticException { Stage: SqlTestDiagnosticStage.TARGET_IDENTITY } known
                ? known.Reason ?? fallback : fallback
            : (SqlTargetIdentityReason?)null;
        return new SqlTestDiagnosticException(stage, reason);
    }

    // The production connection path and offline fault-injection tests use this same ordering.
    // No later check runs after a failure, and a failed connection is never returned to the caller.
    internal static async Task VerifyConnectionAsync(Func<Task> open, Func<Task> targetIdentity,
        Func<Task> permissions, Func<Task> fingerprint, Func<Task>? writeSchemaShape,
        Func<ValueTask> disposeOnFailure)
    {
        try
        {
            await RunAsync(SqlTestDiagnosticStage.CONNECTION_OPEN, open);
            await RunAsync(SqlTestDiagnosticStage.TARGET_IDENTITY, targetIdentity);
            await RunAsync(SqlTestDiagnosticStage.PERMISSIONS, permissions);
            await RunAsync(SqlTestDiagnosticStage.SCHEMA_FINGERPRINT, fingerprint);
            if (writeSchemaShape is not null)
                await RunAsync(SqlTestDiagnosticStage.SCHEMA_SHAPE, writeSchemaShape);
        }
        catch
        {
            try { await disposeOnFailure(); }
            catch { /* Cleanup must never disclose provider details or replace the failing stage. */ }
            throw;
        }
    }
}
