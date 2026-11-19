using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudioManager.CustomerApi;

/// <summary>Explicit repository Debug layout, separate from the immutable packaged demo configuration.</summary>
public static class LocalDemoDevelopment
{
    public const string Option = "--local-demo-dev";
    public const string SettingsFileName = "local-demo.development.json";

    internal static LocalDemoProfile Select(string? executable, LocalDemoComponent component)
    {
        if (executable is null || component is not (LocalDemoComponent.Staff or LocalDemoComponent.CustomerClient))
            throw new InvalidOperationException("Local Demo development startup supports the Staff and CustomerClient Debug apphosts only.");
        var fullPath = Path.GetFullPath(executable);
        var project = Directory.GetParent(fullPath)?.Parent?.Parent?.Parent;
        var root = project?.Parent?.Parent?.FullName;
        if (root is null || !File.Exists(Path.Combine(root, "StudioManager.sln")) ||
            !File.Exists(Path.Combine(root, "src", ProjectName(component), ProjectName(component) + ".csproj")) ||
            !string.Equals(fullPath, Executable(root, component), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the source project bin/Debug/net8.0-windows apphost for Local Demo F5; custom/Release output paths are refused.");

        var settingsPath = Path.Combine(root, SettingsFileName);
        LocalDemoProfile.RejectReparsePoints(settingsPath);
        try
        {
            var info = new FileInfo(settingsPath);
            if (!info.Exists || info.Length is <= 0 or > 4096) throw new InvalidOperationException();
            var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsPath), new JsonSerializerOptions
                { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow });
            if (settings?.Profile != LocalDemoProfile.ProfileName || string.IsNullOrWhiteSpace(settings.RuntimeFile) ||
                Path.IsPathRooted(settings.RuntimeFile)) throw new InvalidOperationException();
            var runtime = Path.GetFullPath(Path.Combine(root, settings.RuntimeFile));
            var artifacts = Path.Combine(root, "artifacts") + Path.DirectorySeparatorChar;
            if (!runtime.StartsWith(artifacts, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException();
            var selected = LocalDemoProfile.Parse(["--local-demo", "--runtime", runtime])!;
            return new LocalDemoProfile(selected.RuntimeFile, root);
        }
        catch
        {
            throw new InvalidOperationException($"Missing or invalid {SettingsFileName}. It must explicitly select the approved profile and an existing runtime under this repository's artifacts folder. No production fallback was attempted.");
        }
    }

    internal static string Executable(string root, LocalDemoComponent component)
    {
        var project = ProjectName(component);
        return Path.Combine(root, "src", project, "bin", "Debug", "net8.0-windows", project + ".exe");
    }

    private static string ProjectName(LocalDemoComponent component) => component switch
    {
        LocalDemoComponent.Staff => "StudioManager.WinForms",
        LocalDemoComponent.CustomerClient => "StudioManager.CustomerClient",
        _ => throw new InvalidOperationException("A GUI development component is required.")
    };

    private sealed record Settings(string Profile, string RuntimeFile);
}
