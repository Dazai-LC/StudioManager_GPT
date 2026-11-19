using System.Diagnostics;
using System.Reflection;
using System.Reflection.PortableExecutable;
using StudioManager.CustomerApi;

namespace StudioManager.WinForms;

/// <summary>Launch plan only; never searches arbitrary folders or substitutes the legacy mock portal.</summary>
internal static class CustomerPortalLauncher
{
    internal static ProcessStartInfo Prepare(string? staffExecutable, LocalDemoProfile? demo, bool registration)
    {
        if (staffExecutable is null) throw Unavailable();
        var staff = Path.GetFullPath(staffExecutable);
        string customer;
        var arguments = new List<string>();
        if (demo is not null)
        {
            var selected = demo.LaunchArguments;
            var staffProfile = LocalDemoProfile.SelectForProcess(selected, staff, LocalDemoComponent.Staff)!;
            customer = demo.Executable(LocalDemoComponent.CustomerClient);
            var customerProfile = LocalDemoProfile.SelectForProcess(selected, customer, LocalDemoComponent.CustomerClient)!;
            if (!string.Equals(staffProfile.RuntimeFile, demo.RuntimeFile, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(customerProfile.RuntimeFile, demo.RuntimeFile, StringComparison.OrdinalIgnoreCase)) throw Unavailable();
            arguments.AddRange(selected);
        }
        else
        {
            LocalDemoProfile.SelectForProcess([], staff, LocalDemoComponent.Staff);
            var directory = Path.GetDirectoryName(staff)!;
            if (!string.Equals(Path.GetFileName(directory), "Staff", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetFileName(staff), "StudioManager.WinForms.exe", StringComparison.OrdinalIgnoreCase)) throw Unavailable();
            customer = Path.Combine(Path.GetDirectoryName(directory)!, "CustomerClient", "StudioManager.CustomerClient.exe");
        }
        RejectRedirectedPath(staff); RejectRedirectedPath(customer);
        var folder = Path.GetDirectoryName(customer)!;
        if (!File.Exists(customer) || !File.Exists(Path.Combine(folder, "StudioManager.CustomerClient.runtimeconfig.json")) ||
            !File.Exists(Path.Combine(folder, "StudioManager.CustomerClient.deps.json")) ||
            !File.Exists(Path.Combine(folder, "StudioManager.CustomerApi.dll")) ||
            AssemblyName.GetAssemblyName(Path.Combine(folder, "StudioManager.CustomerClient.dll")).Name != "StudioManager.CustomerClient") throw Unavailable();
        using (var stream = File.OpenRead(customer))
        using (var pe = new PEReader(stream))
            if (pe.PEHeaders.PEHeader?.Subsystem != Subsystem.WindowsGui) throw Unavailable();
        if (registration) arguments.Add("--register");
        var info = new ProcessStartInfo(customer) { UseShellExecute = false, WorkingDirectory = folder };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        return info;
    }

    private static void RejectRedirectedPath(string path)
    {
        if (path.StartsWith(@"\\", StringComparison.Ordinal)) throw Unavailable();
        for (var current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw Unavailable();
    }
    private static InvalidOperationException Unavailable() => new("Không tìm thấy CustomerClient thật trong bố trí Staff/CustomerClient hoặc source Debug Local Demo. Build lại solution Debug và kiểm tra profile.");
}
