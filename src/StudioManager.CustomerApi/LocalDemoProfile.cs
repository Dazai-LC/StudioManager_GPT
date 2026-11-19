using System.Runtime.Versioning;

namespace StudioManager.CustomerApi;

public enum LocalDemoComponent { IdentityService, CustomerClient, Staff }

/// <summary>Explicit, process-local profile. No environment-variable or production endpoint fallback.</summary>
public sealed class LocalDemoProfile
{
    public const string ProfileName = "StudioManager.LocalDemo.20261116A.v1";
    public const string PipeName = "StudioManager.Identity.LocalDemo.20261116A.v1";
    public const string RuntimeFileName = "local-demo-host.json";
    public const string MarkerFileName = "local-demo.profile";
    public string RuntimeFile { get; }
    internal string? DevelopmentRoot { get; }
    public bool IsDevelopment => DevelopmentRoot is not null;
    public string RootDirectory => Path.GetDirectoryName(RuntimeFile)!;
    public string HostExecutable => Executable(LocalDemoComponent.IdentityService);
    public string[] LaunchArguments => IsDevelopment ? [LocalDemoDevelopment.Option] : ["--local-demo", "--runtime", RuntimeFile];

    internal LocalDemoProfile(string runtimeFile, string? developmentRoot = null)
    { RuntimeFile = runtimeFile; DevelopmentRoot = developmentRoot; }

    public static LocalDemoProfile? Parse(string[] args)
    {
        if (args.Length == 0) return null;
        if (args.Length != 3 || args[0] != "--local-demo" || args[1] != "--runtime")
            throw new InvalidOperationException("Use --local-demo --runtime <absolute local-demo-host.json path>; no other options are supported.");
        var path = args[2];
        if (!Path.IsPathFullyQualified(path) || path.StartsWith(@"\\", StringComparison.Ordinal) ||
            path.AsSpan(2).Contains(':') ||
            !string.Equals(Path.GetFileName(path), RuntimeFileName, StringComparison.Ordinal))
            throw new InvalidOperationException("An absolute local runtime file path with the approved filename is required.");
        return new LocalDemoProfile(Path.GetFullPath(path));
    }

    public static LocalDemoProfile? SelectForProcess(string[] args, string? executable, LocalDemoComponent component, bool requireLocalDemo = false)
    {
        var profile = args is [LocalDemoDevelopment.Option]
            ? LocalDemoDevelopment.Select(executable, component) : Parse(args);
        var parent = executable is null ? null : Path.GetDirectoryName(Path.GetDirectoryName(executable));
        if (profile is null)
        {
            // A demo output must not switch to the installed P03 pipe when its arguments are accidentally omitted.
            if (requireLocalDemo || (parent is not null && File.Exists(Path.Combine(parent, MarkerFileName))))
                throw new InvalidOperationException("Debug/demo startup requires an explicit Local Demo launch profile; select the Local Demo F5 profile. Production fallback is refused.");
            return null;
        }
        profile.ValidateExecutable(executable, component);
        profile.ValidateMarker();
        return profile;
    }

    internal void ValidateMarker()
    {
        RejectReparsePoints(RuntimeFile);
        var marker = Path.Combine(RootDirectory, MarkerFileName);
        RejectReparsePoints(marker);
        if (!File.Exists(marker) || new FileInfo(marker).Length > 128 || File.ReadAllText(marker).Trim() != ProfileName)
            throw new InvalidOperationException("The matching local demo profile marker is required in the separate runtime root.");
    }

    public string Executable(LocalDemoComponent component) => IsDevelopment && component != LocalDemoComponent.IdentityService
        ? LocalDemoDevelopment.Executable(DevelopmentRoot!, component) : component switch
    {
        LocalDemoComponent.IdentityService => Path.Combine(RootDirectory, "IdentityService", "StudioManager.IdentityService.exe"),
        LocalDemoComponent.CustomerClient => Path.Combine(RootDirectory, "CustomerClient", "StudioManager.CustomerClient.exe"),
        LocalDemoComponent.Staff => Path.Combine(RootDirectory, "Staff", "StudioManager.WinForms.exe"),
        _ => throw new InvalidOperationException("Unknown local demo component.")
    };

    // Development reads the approved settings in place; it never copies production appsettings into the demo.
    public string ConfigurationDirectory(LocalDemoComponent component) => Path.Combine(RootDirectory, component switch
    {
        LocalDemoComponent.Staff => "Staff",
        LocalDemoComponent.IdentityService => "IdentityService",
        LocalDemoComponent.CustomerClient => "CustomerClient",
        _ => throw new InvalidOperationException("Unknown local demo component.")
    });

    public void ValidateExecutable(string? executable, LocalDemoComponent component)
    {
        if (executable is null || !string.Equals(Path.GetFullPath(executable), Executable(component), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The local demo must use its matching packaged executable or explicitly selected source Debug output.");
        RejectReparsePoints(executable);
    }

    internal static void RejectReparsePoints(string path)
    {
        for (var entry = Path.GetFullPath(path); !string.IsNullOrEmpty(entry); entry = Path.GetDirectoryName(entry))
            if ((File.Exists(entry) || Directory.Exists(entry)) && (File.GetAttributes(entry) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Local demo paths must not contain reparse points.");
    }
}

/// <summary>One choice before any form/client is created; rejected configuration cannot select production.</summary>
[SupportedOSPlatform("windows")]
public static class LocalDemoClientRuntime
{
    private static readonly object Gate = new();
    private static bool _configured, _rejected;
    private static LocalDemoProfile? _profile;

    public static LocalDemoProfile? Configure(string[] args, LocalDemoComponent component, bool requireLocalDemo = false)
    {
        lock (Gate)
        {
            if (_configured) throw new InvalidOperationException("Client runtime has already been selected.");
            _configured = true; _rejected = true;
            if (component is not (LocalDemoComponent.CustomerClient or LocalDemoComponent.Staff))
                throw new InvalidOperationException("A client runtime component is required.");
            var profile = LocalDemoProfile.SelectForProcess(args, Environment.ProcessPath, component, requireLocalDemo);
            if (profile is not null)
            {
                if (!string.Equals(Environment.MachineName, "MSI", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Local demo requires the approved MSI machine.");
                if (profile.IsDevelopment) LocalDemoHostStartup.EnsureRunning(profile);
                else LocalDemoHostIdentity.Read(profile); // Packaged startup retains its explicit host workflow.
            }
            _profile = profile; _rejected = false;
            return profile;
        }
    }

    internal static LocalDemoProfile? Current
    {
        get
        {
            lock (Gate)
            {
                if (_rejected) throw new InvalidOperationException("Client runtime selection failed; restart with valid configuration.");
                return _profile;
            }
        }
    }
}
