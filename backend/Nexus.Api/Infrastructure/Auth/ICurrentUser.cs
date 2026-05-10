namespace Nexus.Api.Infrastructure.Auth;

public interface ICurrentUser
{
    string Id { get; }
    string Name { get; }
    bool IsAdmin { get; }
}

public class HardcodedAdminUser : ICurrentUser
{
    public string Id => "admin";
    public string Name => "Admin";
    public bool IsAdmin => true;
}
