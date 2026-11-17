using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;

namespace StudioManager.Infrastructure;

/// <summary>Only used after explicit demo selection. Parsing never opens a SQL connection.</summary>
public static class LocalDemoSqlSettings
{
    public const string Server = @"MSI\SQLEXPRESS";
    public const string Database = "StudioManager_P04_Test_20261116A";

    public static string Load(string directory)
    {
        try { return ValidateJson(File.ReadAllText(Path.Combine(directory, "appsettings.json"))); }
        catch { throw InvalidConfiguration(); }
    }

    public static string ValidateJson(string json)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<Settings>(json, new JsonSerializerOptions
                { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow });
            var sql = new SqlConnectionStringBuilder(settings?.ConnectionString ?? "");
            if (sql.DataSource != Server || sql.InitialCatalog != Database || !sql.IntegratedSecurity ||
                sql.Encrypt != SqlConnectionEncryptOption.Mandatory || !sql.TrustServerCertificate ||
                !string.IsNullOrEmpty(sql.UserID) || !string.IsNullOrEmpty(sql.Password) ||
                !string.IsNullOrEmpty(sql.AttachDBFilename) || sql.UserInstance ||
                !string.IsNullOrEmpty(sql.FailoverPartner) || !string.IsNullOrEmpty(sql.HostNameInCertificate) ||
                !string.IsNullOrEmpty(sql.ServerCertificate) || sql.ApplicationIntent != ApplicationIntent.ReadWrite ||
                !string.IsNullOrEmpty(sql.ServerSPN) || !string.IsNullOrEmpty(sql.FailoverPartnerSPN) ||
                sql.Authentication != SqlAuthenticationMethod.NotSpecified)
                throw InvalidConfiguration();
            return sql.ConnectionString;
        }
        catch { throw InvalidConfiguration(); }
    }

    private static InvalidOperationException InvalidConfiguration() => new(
        "Local demo appsettings.json must select MSI\\SQLEXPRESS / StudioManager_P04_Test_20261116A with Windows authentication, Encrypt=True and TrustServerCertificate=True; no credentials or alternate target are permitted.");
    private sealed record Settings(string ConnectionString);
}
