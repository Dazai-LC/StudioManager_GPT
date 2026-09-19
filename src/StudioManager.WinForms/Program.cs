using System.Text.Json;
using StudioManager.Application;
using StudioManager.Application.Services;
using StudioManager.Infrastructure;
using StudioManager.WinForms.Forms;

namespace StudioManager.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        System.Windows.Forms.Application.ThreadException += (_, e) => MessageBox.Show("Đã xảy ra lỗi: " + e.Exception.Message, "Studio Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
        var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))) ?? new();
        var facade = new AppFacade(new SqlStudioRepository(settings.ConnectionString), new Pbkdf2PasswordHasher(), new SystemClock());
        System.Windows.Forms.Application.Run(new LoginForm(facade));
    }
    private sealed class AppSettings { public string ConnectionString { get; set; } = ""; }
}
