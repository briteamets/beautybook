using System.Text.Json;

namespace ClientApp.Services;

public static class SessionTokenStore
{
    private const string JwtKey = "ClientJwt";
    private const string RefreshTokenKey = "ClientRefreshToken";

    public static string? GetJwt(this ISession session) => session.GetString(JwtKey);
    public static string? GetRefreshToken(this ISession session) => session.GetString(RefreshTokenKey);

    public static void SetTokens(this ISession session, string jwt, string refreshToken)
    {
        session.SetString(JwtKey, jwt);
        session.SetString(RefreshTokenKey, refreshToken);
    }

    public static void ClearTokens(this ISession session)
    {
        session.Remove(JwtKey);
        session.Remove(RefreshTokenKey);
    }

    public static bool HasUsableToken(this ISession session)
    {
        return !string.IsNullOrWhiteSpace(session.GetJwt()) && !string.IsNullOrWhiteSpace(session.GetRefreshToken());
    }

    public static bool IsJwtExpired(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2) return true;

        var payload = parts[1]
            .Replace('-', '+')
            .Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

        var json = JsonDocument.Parse(Convert.FromBase64String(payload));
        if (!json.RootElement.TryGetProperty("exp", out var expElement)) return true;

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expElement.GetInt64()).UtcDateTime;
        return expiresAt <= DateTime.UtcNow.AddMinutes(1);
    }
}
