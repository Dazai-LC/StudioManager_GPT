using System.Text.RegularExpressions;
using Xunit;

namespace StudioManager.Tests;

public sealed class FrontendV22Tests
{
    private static string ResolveRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "StudioManager.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not resolve StudioManager.sln root directory.");
    }

    private static string WinFormsRoot() => Path.Combine(ResolveRepoRoot(), "src", "StudioManager.WinForms");

    [Fact]
    public void VisualAssetInventory_HasAll25OfflineJpgFilesWithNonEmptyPayloads()
    {
        var assetsRoot = Path.Combine(WinFormsRoot(), "Assets");
        var expectedRelativePaths = new[]
        {
            // 2 Auth Backgrounds (AI photo)
            Path.Combine("Backgrounds", "bg_auth_desktop_login.jpg"),
            Path.Combine("Backgrounds", "bg_auth_portal_register.jpg"),
            // 5 Catalog Packages (GC000001..05)
            Path.Combine("MediaDefaults", "packages", "pkg_GC000001_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "packages", "pkg_GC000002_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "packages", "pkg_GC000003_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "packages", "pkg_GC000004_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "packages", "pkg_GC000005_cover_ai.jpg"),
            // 4 Studio Rooms (P0001..03 + live SQL P5A94A4)
            Path.Combine("MediaDefaults", "rooms", "room_P0001_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "rooms", "room_P0002_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "rooms", "room_P0003_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "rooms", "room_P5A94A4_cover_ai.jpg"),
            // 12 Studio Resources (TN000001..10 + live SQL TNA1DC12, TN501515)
            Path.Combine("MediaDefaults", "resources", "res_TN000001_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "resources", "res_TN000002_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "resources", "res_TN000003_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "resources", "res_TN000004_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "resources", "res_TN000005_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "resources", "res_TN000006_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "resources", "res_TN000007_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "resources", "res_TN000008_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "resources", "res_TN000009_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "resources", "res_TN000010_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "resources", "res_TNA1DC12_cover_ai.jpg"),
            Path.Combine("MediaDefaults", "resources", "res_TN501515_cover_ai.jpg"),
            // 2 Fallback Artwork Plates
            Path.Combine("MediaDefaults", "fallbacks", "fallback_catalog_cover.jpg"),
            Path.Combine("MediaDefaults", "fallbacks", "fallback_atelier_square.jpg")
        };

        Assert.Equal(25, expectedRelativePaths.Length);

        foreach (var rel in expectedRelativePaths)
        {
            var full = Path.Combine(assetsRoot, rel);
            Assert.True(File.Exists(full), $"Missing offline image asset: {rel}");
            var len = new FileInfo(full).Length;
            Assert.True(len > 10_000, $"Unexpectedly small asset file ({len} bytes): {rel}");
        }
    }

    [Fact]
    public void WinFormsProject_IncludesAssetsDirectoryCopyToOutput()
    {
        var csprojPath = Path.Combine(WinFormsRoot(), "StudioManager.WinForms.csproj");
        var text = File.ReadAllText(csprojPath);
        Assert.Contains(@"Assets\**\*", text);
        Assert.Contains("PreserveNewest", text);
    }

    [Fact]
    public void WinFormsCodeAndUiLiterals_NeverExposeSePayIntegrationOrSdk()
    {
        var csFiles = Directory.GetFiles(WinFormsRoot(), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                        !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToList();

        Assert.NotEmpty(csFiles);

        foreach (var file in csFiles)
        {
            var nonCommentLines = File.ReadAllLines(file)
                .Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal));
            var body = string.Join('\n', nonCommentLines);
            Assert.DoesNotMatch(new Regex("sepay", RegexOptions.IgnoreCase), body);
        }
    }

    [Fact]
    public void CustomerUiFiles_NeverExposeInternalEngineeringCodes()
    {
        var customerFolders = new[]
        {
            Path.Combine(WinFormsRoot(), "CustomerPages"),
            Path.Combine(WinFormsRoot(), "Forms")
        };

        var files = customerFolders
            .Where(Directory.Exists)
            .SelectMany(d => Directory.GetFiles(d, "*.cs", SearchOption.TopDirectoryOnly))
            .ToList();

        Assert.NotEmpty(files);

        var forbidden = new Regex("DEC-03|CTR-|BG-AUTH-", RegexOptions.IgnoreCase);
        foreach (var file in files)
        {
            var nonCommentLines = File.ReadAllLines(file)
                .Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal));
            var body = string.Join('\n', nonCommentLines);
            Assert.DoesNotMatch(forbidden, body);
        }
    }

    [Fact]
    public void DesktopOnlySolution_PreservesDocumentedArchivesWithoutDependingOnUntrackedPrototypes()
    {
        var repoRoot = ResolveRepoRoot();
        var amendment = File.ReadAllText(Path.Combine(repoRoot, "docs", "v2.2", "frontend", "DESKTOP_ONLY_ARCHITECTURE_AMENDMENT.md"));
        var solution = File.ReadAllText(Path.Combine(repoRoot, "StudioManager.sln"));
        var desktopProject = File.ReadAllText(Path.Combine(WinFormsRoot(), "StudioManager.WinForms.csproj"));

        // Archives remain documented; an untracked prototype in a different worktree is not a build prerequisite.
        Assert.Contains("DEC-01-DESKTOP-ONLY", amendment, StringComparison.Ordinal);
        Assert.Contains("archive/v2.2-phase02-nextjs", amendment, StringComparison.Ordinal);
        Assert.Contains("src/StudioManager.CustomerPortal/", amendment, StringComparison.Ordinal);
        Assert.Contains("untracked", amendment, StringComparison.Ordinal);
        Assert.DoesNotContain("StudioManager.CustomerPortal", solution, StringComparison.Ordinal);
        Assert.DoesNotContain("Next.js", solution, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("StudioManager.WinForms", solution, StringComparison.Ordinal);
        Assert.Contains("<UseWindowsForms>true</UseWindowsForms>", desktopProject, StringComparison.Ordinal);
    }

    [Fact]
    public void WinFormsCode_UsesBrandedAtelierNotificationsInsteadOfNativeMessageBox()
    {
        var csFiles = Directory.GetFiles(WinFormsRoot(), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                        !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        Assert.NotEmpty(csFiles);
        var offenders = new List<string>();
        foreach (var file in csFiles)
        {
            foreach (var rawLine in File.ReadAllLines(file))
            {
                var line = rawLine.TrimStart();
                if (line.StartsWith("//") || line.StartsWith("///") || line.StartsWith("*")) continue;
                if (line.Contains("MessageBox.Show(", StringComparison.Ordinal))
                    offenders.Add($"{Path.GetFileName(file)}: {line}");
            }
        }

        Assert.Empty(offenders);
        var notifierPath = Path.Combine(WinFormsRoot(), "Controls", "AtelierNotifications.cs");
        Assert.True(File.Exists(notifierPath), "Branded notification service file missing.");
    }

    [Fact]
    public void VietQrCardControl_RemainsExplicitlyNonScannableDemoPlate()
    {
        var src = File.ReadAllText(Path.Combine(WinFormsRoot(), "Controls", "VietQrCardControl.cs"));
        Assert.Contains("DrawNonScannableDemoPlate", src, StringComparison.Ordinal);
        Assert.Contains("QR MINH HỌA UI", src, StringComparison.Ordinal);
        Assert.Contains("KHÔNG QUÉT ĐỂ", src, StringComparison.Ordinal);
        Assert.DoesNotContain("IsFinder", src, StringComparison.Ordinal);
    }

    [Fact]
    public void DesktopDemoSessionStore_EnforcesPerCustomerIsolationAndBookingSync()
    {
        var src = File.ReadAllText(Path.Combine(WinFormsRoot(), "Simulation", "DesktopDemoSessionStore.cs"));
        Assert.DoesNotContain("demoCustomerId == \"KH_DEMO_001\"", src, StringComparison.Ordinal);
        Assert.Contains("\"KH_DEMO_003\"", src, StringComparison.Ordinal);
        Assert.Contains("UpsertLinkedBookingLocked", src, StringComparison.Ordinal);
        Assert.Contains("BuildReceiptForPayment", src, StringComparison.Ordinal);
        Assert.Contains("SubmitCustomerIdentityVerificationRequest", src, StringComparison.Ordinal);
    }

    [Fact]
    public void UnifiedAuthenticationContract_ReplacesDualLoginModeSwitcher()
    {
        var contractsSrc = File.ReadAllText(Path.Combine(WinFormsRoot(), "Contracts", "CustomerIdentityContracts.cs"));
        var loginSrc = File.ReadAllText(Path.Combine(WinFormsRoot(), "Forms", "LoginForm.cs"));
        var custLoginSrc = File.ReadAllText(Path.Combine(WinFormsRoot(), "Forms", "CustomerLoginForm.cs"));

        Assert.Contains("interface IUnifiedAuthenticationService", contractsSrc, StringComparison.Ordinal);
        Assert.Contains("class UnifiedAuthRouter", contractsSrc, StringComparison.Ordinal);
        Assert.Contains("UnifiedAuthRouter.ResolveRoute", loginSrc, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildAccessModeBar", loginSrc, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildAccessModeBar", custLoginSrc, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomerProfilePage_IsolatesQaStateSimulatorInsideCollapsedDrawer()
    {
        var src = File.ReadAllText(Path.Combine(WinFormsRoot(), "CustomerPages", "CustomerProfilePage.cs"));
        Assert.Contains("_qaToggleBtn", src, StringComparison.Ordinal);
        Assert.Contains("_qaDrawerBody = new Panel { Dock = DockStyle.Fill, Visible = false", src, StringComparison.Ordinal);
        Assert.Contains("SubmitCustomerIdentityVerificationRequest", src, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomerDashboardPage_IsolatesNotificationsByCustomerAccount()
    {
        var src = File.ReadAllText(Path.Combine(WinFormsRoot(), "CustomerPages", "CustomerDashboardPage.cs"));
        Assert.Contains("DesktopDemoSessionStore.GetNotifications(_session.Profile.DemoAccountId)", src, StringComparison.Ordinal);
        Assert.DoesNotContain("DesktopDemoSessionStore.GetNotifications()", src, StringComparison.Ordinal);
    }

    [Fact]
    public void SeedGroundedCatalogRepository_SupportsLiveSqlRoomsAndResourcesQueries()
    {
        var src = File.ReadAllText(Path.Combine(WinFormsRoot(), "Simulation", "SeedGroundedCatalogRepository.cs"));
        Assert.Contains("TryLoadLiveRoomsReadOnlyAsync", src, StringComparison.Ordinal);
        Assert.Contains("TryLoadLiveResourcesReadOnlyAsync", src, StringComparison.Ordinal);
        Assert.Contains("\"PhongChup\"", src, StringComparison.Ordinal);
        Assert.Contains("\"TaiNguyen\"", src, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomerPackagesPage_UsesModernDataGridViewInsteadOfLegacyListView()
    {
        var src = File.ReadAllText(Path.Combine(WinFormsRoot(), "CustomerPages", "CustomerPackagesPage.cs"));
        Assert.Contains("DataGridView _compareGrid = Theme.Grid()", src, StringComparison.Ordinal);
        Assert.DoesNotContain("new ListView", src, StringComparison.Ordinal);
    }

    [Fact]
    public void VisualAssetService_LabelsAll21RealPhotographyTargetsAndExplainsUnknownFallback()
    {
        var src = File.ReadAllText(Path.Combine(WinFormsRoot(), "Simulation", "VisualAssetService.cs"));
        string[] expectedCodes =
        [
            "\"GC000001\"", "\"GC000002\"", "\"GC000003\"", "\"GC000004\"", "\"GC000005\"",
            "\"P0001\"", "\"P0002\"", "\"P0003\"", "\"P5A94A4\"",
            "\"TN000001\"", "\"TN000002\"", "\"TN000003\"", "\"TN000004\"", "\"TN000005\"",
            "\"TN000006\"", "\"TN000007\"", "\"TN000008\"", "\"TN000009\"", "\"TN000010\"",
            "\"TNA1DC12\"", "\"TN501515\""
        ];
        foreach (var code in expectedCodes)
            Assert.Contains(code, src, StringComparison.Ordinal);
        Assert.Contains("Chưa có ảnh chụp riêng cho mã", src, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomerMainForm_HasSpaciousHeaderAndCrudPageMatchesCustomRoomPrefixes()
    {
        var customerShellSrc = File.ReadAllText(Path.Combine(WinFormsRoot(), "Forms", "CustomerMainForm.cs"));
        Assert.Contains("RowStyle(SizeType.Absolute, 80)", customerShellSrc, StringComparison.Ordinal);
        Assert.Contains("_topBreadcrumbLabel", customerShellSrc, StringComparison.Ordinal);
        Assert.Contains("_topTitleLabel", customerShellSrc, StringComparison.Ordinal);

        var crudSrc = File.ReadAllText(Path.Combine(WinFormsRoot(), "Pages", "CrudPage.cs"));
        Assert.Contains("raw.StartsWith(\"P\", StringComparison.OrdinalIgnoreCase)", crudSrc, StringComparison.Ordinal);
        Assert.DoesNotContain("raw.StartsWith(\"P0\", StringComparison.OrdinalIgnoreCase)", crudSrc, StringComparison.Ordinal);
    }
}

