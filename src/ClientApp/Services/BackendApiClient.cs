using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Threading;
using App.DTO.v1.Identity;

namespace ClientApp.Services;

public class BackendApiClient
{
    private readonly HttpClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BackendApiClient(HttpClient client, IHttpContextAccessor httpContextAccessor)
    {
        _client = client;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<T?> GetAsync<T>(string url) => await SendAsync<T>(HttpMethod.Get, url);
    public async Task<T?> PostAsync<T>(string url, object body) => await SendAsync<T>(HttpMethod.Post, url, body);
    public async Task<T?> PutAsync<T>(string url, object body) => await SendAsync<T>(HttpMethod.Put, url, body);
    public async Task<T?> PatchAsync<T>(string url, object body) => await SendAsync<T>(HttpMethod.Patch, url, body);
    public async Task DeleteAsync(string url) => await SendAsync<object>(HttpMethod.Delete, url);

    public async Task<T?> AnonymousGetAsync<T>(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(
            Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            return default;
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<T?> AnonymousPostAsync<T>(string url, object body)
    {
        var response = await _client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<T?> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        await EnsureFreshJwtAsync();

        using var request = new HttpRequestMessage(method, url);
        var jwt = _httpContextAccessor.HttpContext!.Session.GetJwt();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(
            Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName));

        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }

        var response = await _client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _httpContextAccessor.HttpContext!.Session.ClearTokens();
            throw new UnauthorizedAccessException();
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"{(int)response.StatusCode} {response.StatusCode}: {errorBody}", null, response.StatusCode);
        }

        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            return default;

        return await response.Content.ReadFromJsonAsync<T>();
    }

    // jwt refresh
    // Kui token aegub vähem kui 1 minuti pärast, loetakse see aegunuks ja uuendatakse kohe.
    private async Task EnsureFreshJwtAsync()
    {
        var session = _httpContextAccessor.HttpContext!.Session;
        var jwt = session.GetJwt();
        var refreshToken = session.GetRefreshToken();

        if (string.IsNullOrWhiteSpace(jwt) || string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException();
        }

        if (!SessionTokenStore.IsJwtExpired(jwt)) return;

        var response = await _client.PostAsJsonAsync("/api/v1/identity/account/refreshtokendata",
            new TokenRefreshInfo
            {
                Jwt = jwt,
                RefreshToken = refreshToken
            });
        if (!response.IsSuccessStatusCode)
        {
            session.ClearTokens();
            throw new UnauthorizedAccessException();
        }

        var tokens = await response.Content.ReadFromJsonAsync<JWTResponse>();
        if (tokens == null) throw new UnauthorizedAccessException();

        session.SetTokens(tokens.Jwt, tokens.RefreshToken);
    }
}
