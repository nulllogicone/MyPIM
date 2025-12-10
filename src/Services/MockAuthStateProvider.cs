using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace MyPIM.Services;

public class MockAuthStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "user-123")
        }, "MockAuth");

        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }
}
