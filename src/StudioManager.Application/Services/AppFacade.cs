using StudioManager.Domain;

namespace StudioManager.Application.Services;

public sealed class AppFacade(IStudioRepository repository, IPasswordHasher hasher, IClock clock)
{
    // Repository stays inside Application. WinForms must only use the services below.
    public AuthService Auth { get; } = new(repository, hasher, clock);
    public BookingService Bookings { get; } = new(repository, clock);
    public FinanceService Finance { get; } = new(repository);
    public AdministrationService Administration { get; } = new(repository, hasher);
    public BookingSupportService BookingSupport { get; } = new(repository);
    public DashboardService Dashboard { get; } = new(repository);
    public ReportingService Reports { get; } = new(repository);
    public BackupRestoreService BackupRestore { get; } = new(repository);
    public UserSession? Session { get; set; }
}
