namespace Samples.Showcase.Shared.Services;

public interface IProfileService
{
    string? CurrentUserName { get; }
    bool IsLoggedIn { get; }
    void Login(string userName);
    void Logout();
}

public sealed class FakeProfileService : IProfileService
{
    public string? CurrentUserName { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUserName);

    public void Login(string userName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        CurrentUserName = userName.Trim();
    }

    public void Logout() => CurrentUserName = null;
}
