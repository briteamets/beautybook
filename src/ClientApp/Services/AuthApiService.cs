using App.DTO.v1.Identity;

namespace ClientApp.Services;

// teeb päringu AccountControllerile
public class AuthApiService
{
    private readonly BackendApiClient _api;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthApiService(BackendApiClient api, IHttpContextAccessor httpContextAccessor)
    {
        _api = api;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LoginAsync(string email, string password)
    {
        var tokens = await _api.AnonymousPostAsync<JWTResponse>("/api/v1/identity/account/login",
            new LoginInfo { Email = email, Password = password });
        if (tokens == null) throw new UnauthorizedAccessException();

        _httpContextAccessor.HttpContext!.Session.SetTokens(tokens.Jwt, tokens.RefreshToken); // edukal juhul salvestatakse
        // mõlemad tokenid serveripoolsesse sessiooni — JWT võtmega "ClientJwt" ja refresh token võtmega "ClientRefreshToken"
    }

    public async Task LogoutAsync()
    {
        var session = _httpContextAccessor.HttpContext!.Session;
        var refreshToken = session.GetRefreshToken();
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                await _api.PostAsync<object>("/api/v1/identity/account/logout", new LogoutInfo { RefreshToken = refreshToken });
            }
            catch
            {
                // Local logout must still clear the session even if the API token is already invalid.
            }
        }

        session.ClearTokens();
    }
}
