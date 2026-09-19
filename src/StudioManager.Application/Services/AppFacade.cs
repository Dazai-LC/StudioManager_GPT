using StudioManager.Domain;

namespace StudioManager.Application.Services;

public sealed class AppFacade(IStudioRepository repository, IPasswordHasher hasher, IClock clock)
{
    public IStudioRepository Repository { get; } = repository;
    public IPasswordHasher PasswordHasher { get; } = hasher;
    public AuthService Auth { get; } = new(repository, hasher, clock);
    public BookingService Bookings { get; } = new(repository, clock);
    public FinanceService Finance { get; } = new(repository);
    public UserSession? Session { get; set; }
}
