namespace App.DAL.EF.Seeding;

public static class InitialData
{
    public static readonly string[] Roles = [
        "Admin",
        "Client",
        "User",
        "ServiceProvider"
    ];

    public static readonly (string email, string password, string[] roles)[] Users = [
        ("admin@beautybook.com", "Admin123!", ["Admin"]),
        ("user@beautybook.com", "User123!", ["Client", "User"]),
        ("client2@beautybook.com", "User123!", ["Client", "User"]),
        ("provider@beautybook.com", "Provider123!", ["ServiceProvider"]),
        ("provider2@beautybook.com", "Provider123!", ["ServiceProvider"]),
        ("provider3@beautybook.com", "Provider123!", ["ServiceProvider"]),
        ("suspended.provider@beautybook.com", "Provider123!", ["ServiceProvider"]),
    ];
    
}
