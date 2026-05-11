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

    public static string? GetDisplayName(this ISession session)
    {
        var jwt = session.GetJwt();
        if (string.IsNullOrWhiteSpace(jwt)) return null;
        var payload = DecodePayload(jwt);
        if (payload == null) return null;

        var given = TryGet(payload, "given_name",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname");
        var family = TryGet(payload, "family_name",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname");

        if (!string.IsNullOrWhiteSpace(given) || !string.IsNullOrWhiteSpace(family))
            return $"{given} {family}".Trim();

        return TryGet(payload, "email",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
            "unique_name",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
    }

    private static string? TryGet(JsonDocument doc, params string[] keys)
    {
        foreach (var key in keys)
            if (doc.RootElement.TryGetProperty(key, out var v))
                return v.GetString();
        return null;
    }

    public static bool IsJwtExpired(string jwt)
    {
        var json = DecodePayload(jwt);
        if (json == null) return true;
        if (!json.RootElement.TryGetProperty("exp", out var expElement)) return true;
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expElement.GetInt64()).UtcDateTime;
        return expiresAt <= DateTime.UtcNow.AddMinutes(1);
    }

    private static JsonDocument? DecodePayload(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2) return null;
        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        try { return JsonDocument.Parse(Convert.FromBase64String(payload)); }
        catch { return null; }
    }
}
