using StudioManager.Domain;

namespace StudioManager.Application.Services;

public sealed class AppFacade
{
    public AppFacade(IStudioRepository repository, IPasswordHasher hasher, IClock clock)
    {
        var access = new InternalAuthorization(repository, clock);
        Auth = new(repository, hasher, clock, access);
        Bookings = new(repository, clock, access);
        Finance = new(repository, access);
        Administration = new(repository, hasher, access);
        BookingSupport = new(repository, access);
        Dashboard = new(repository, access);
        Reports = new(repository, access);
        BackupRestore = new(repository, access);
    }
    // Repository stays inside Application. WinForms must only use the services below.
    public AuthService Auth { get; }
    public BookingService Bookings { get; }
    public FinanceService Finance { get; }
    public AdministrationService Administration { get; }
    public BookingSupportService BookingSupport { get; }
    public DashboardService Dashboard { get; }
    public ReportingService Reports { get; }
    public BackupRestoreService BackupRestore { get; }
    public UserSession? Session => Auth.CurrentSession;
}
