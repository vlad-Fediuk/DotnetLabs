using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AuthService.Features.Auth.DTOs;
using AuthService.Features.Users.DTOs;
using AuthService.IntegrationTests.Infrastructure;
using ECommerce.Shared.Common;
using ECommerce.Shared.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthService.IntegrationTests;

public class AuthAndPbacTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public AuthAndPbacTests(AuthTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithValidData_Returns201WithTokensAndUserPermissions()
    {
        var client = _factory.CreateClient();
        var email = $"user_{Guid.NewGuid():N}@ecommerce.com";

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "SecurePass123!", "Test Buyer"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.RefreshToken.Should().NotBeNullOrWhiteSpace();
        auth.User.Email.Should().Be(email);
        auth.User.Roles.Should().ContainSingle().Which.Should().Be(RoleNames.User);
        auth.User.Permissions.Should().Contain(new[]
        {
            Permissions.Catalog.View,
            Permissions.Orders.Create,
            Permissions.Orders.ViewOwn
        });
        auth.User.Permissions.Should().NotContain(Permissions.Catalog.Manage);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409Conflict()
    {
        var client = _factory.CreateClient();
        var email = $"dup_{Guid.NewGuid():N}@ecommerce.com";

        var first = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "SecurePass123!", "First"));
        var second = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "SecurePass123!", "Second"));

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidPayload_Returns400ValidationProblem()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("not-an-email", "123", ""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(400);
        problem.GetProperty("errors").TryGetProperty("Email", out _).Should().BeTrue();
        problem.GetProperty("errors").TryGetProperty("Password", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithSeededAdmin_Returns200WithFullPermissions()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@ecommerce.com", "Admin123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.User.Roles.Should().Contain(RoleNames.Admin);
        auth.User.Permissions.Should().BeEquivalentTo(Permissions.All);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@ecommerce.com", "WrongPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithDeactivatedAccount_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var email = $"deactivated_{Guid.NewGuid():N}@ecommerce.com";

        var regResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "SecurePass123!", "Deactivated User"));
        var auth = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();

        await _factory.WithDbContextAsync(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Id == auth!.User.Id);
            user.IsActive = false;
            await db.SaveChangesAsync();
        });

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "SecurePass123!"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_RotatesToken_AndRejectsReusedToken()
    {
        var client = _factory.CreateClient();
        var email = $"refresh_{Guid.NewGuid():N}@ecommerce.com";

        var regResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "SecurePass123!", "Refresh User"));
        var initialAuth = (await regResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(initialAuth.RefreshToken));
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotatedAuth = (await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>())!;
        rotatedAuth.RefreshToken.Should().NotBe(initialAuth.RefreshToken);

        var reuseResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(initialAuth.RefreshToken));
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesToken_AndPreventsRefresh()
    {
        var client = _factory.CreateClient();
        var email = $"logout_{Guid.NewGuid():N}@ecommerce.com";

        var regResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "SecurePass123!", "Logout User"));
        var auth = (await regResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var logoutResponse = await client.PostAsJsonAsync("/api/v1/auth/logout", new RefreshTokenRequest(auth.RefreshToken));
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshAfterLogout = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(auth.RefreshToken));
        refreshAfterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsProfileAndPermissions()
    {
        var client = _factory.CreateClient();
        var email = $"me_{Guid.NewGuid():N}@ecommerce.com";
        var regResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "SecurePass123!", "Me User"));
        var auth = (await regResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var meResponse = await client.GetAsync("/api/v1/users/me");

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await meResponse.Content.ReadFromJsonAsync<UserDto>();
        profile.Should().NotBeNull();
        profile!.Id.Should().Be(auth.User.Id);
        profile.Email.Should().Be(email);
        profile.Permissions.Should().Contain(Permissions.Orders.Create);
    }

    [Fact]
    public async Task GetUsers_AsRegularUser_Returns403Forbidden()
    {
        var client = _factory.CreateAuthorizedClient(Guid.NewGuid(), RoleNames.User);

        var response = await client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_AsSupportRoleWithUsersViewPermission_Returns200Ok()
    {
        var client = _factory.CreateAuthorizedClient(Guid.NewGuid(), RoleNames.Support);

        var response = await client.GetAsync("/api/v1/users?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await response.Content.ReadFromJsonAsync<PagedResult<UserDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateUserRoles_AsSupportWithoutManageRolesPermission_Returns403Forbidden()
    {
        var supportClient = _factory.CreateAuthorizedClient(Guid.NewGuid(), RoleNames.Support);
        var targetUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var response = await supportClient.PutAsJsonAsync($"/api/v1/users/{targetUserId}/roles", new UpdateRolesRequest(new[] { RoleNames.User }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateUserRoles_AsAdmin_AssignsNewCustomRoleAndUpdatesPermissions()
    {
        var publicClient = _factory.CreateClient();
        var email = $"promoted_{Guid.NewGuid():N}@ecommerce.com";
        var regResponse = await publicClient.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "SecurePass123!", "Promoted User"));
        var registered = (await regResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        var adminClient = _factory.CreateAuthorizedClient(Guid.NewGuid(), RoleNames.Admin);
        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/v1/users/{registered.User.Id}/roles",
            new UpdateRolesRequest(new[] { RoleNames.CatalogManager }));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var loginResponse = await publicClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "SecurePass123!"));
        var newAuth = (await loginResponse.Content.ReadFromJsonAsync<AuthResponse>())!;
        newAuth.User.Roles.Should().ContainSingle().Which.Should().Be(RoleNames.CatalogManager);
        newAuth.User.Permissions.Should().Contain(Permissions.Catalog.Manage);
        newAuth.User.Permissions.Should().NotContain(Permissions.Users.ManageRoles);
    }
}
