using System.Text.Json;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Infrastructure;
using StudioManager.CustomerApi;
using StudioManager.WinForms.Forms;

namespace StudioManager.WinForms;

internal static class Program
{
#if DEBUG
    private const bool RequireLocalDemo = true;
#else
    private const bool RequireLocalDemo = false;
#endif
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        System.Windows.Forms.Application.ThreadException += (_, e) =>
        {
            Controls.Ui.LogError("Application.ThreadException", e.Exception);
            Controls.AtelierNotificationService.ShowException(
                null,
                "Runtime",
                e.Exception,
                Controls.Ui.FormatRuntimeError("Runtime", e.Exception));
        };
        string connection;
        LocalDemoProfile? demo;
        try
        {
            demo = LocalDemoClientRuntime.Configure(args, LocalDemoComponent.Staff, RequireLocalDemo);
            connection = demo is not null ? LocalDemoSqlSettings.Load(demo.ConfigurationDirectory(LocalDemoComponent.Staff)) :
                (JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))) ?? new()).ConnectionString;
        }
        catch (Exception failure)
        {
            var message = RequireLocalDemo || args.Contains(LocalDemoDevelopment.Option, StringComparer.Ordinal)
                ? "Không thể đọc cấu hình runtime. Chọn profile Staff-Admin (Local Demo), cấu hình Debug.\n\n" + failure.Message
                : "Không thể đọc cấu hình runtime. Với local demo, hãy kiểm tra host, --local-demo --runtime và appsettings.json của bản Staff riêng.";
            using var error = new Controls.AtelierDialog(Controls.AtelierNotificationKind.Error, "Runtime",
                message);
            error.ShowDialog(); Environment.ExitCode = 1; return;
        }
        var facade = new AppFacade(new SqlStudioRepository(connection), new Pbkdf2PasswordHasher(), new SystemClock());
        System.Windows.Forms.Application.Run(new LoginForm(facade, demo));
    }
    private sealed class AppSettings { public string ConnectionString { get; set; } = ""; }
}
