namespace App.DAL.EF.Seeding;

public static class InitialData
{
    public static readonly string[] Roles = [
        "Admin",
        "Client",
        "ServiceProvider",
        "SalonOwner"
    ];

    public static readonly (string email, string password, string[] roles)[] Users = [
        ("admin@beautybook.com", "Admin123!", ["Admin"]),
        ("user@beautybook.com", "User123!", ["Client"]),
        ("client2@beautybook.com", "User123!", ["Client"]),
        ("provider@beautybook.com", "Provider123!", ["ServiceProvider"]),
        ("provider2@beautybook.com", "Provider123!", ["ServiceProvider"]),
        ("provider3@beautybook.com", "Provider123!", ["ServiceProvider"]),
        ("suspended.provider@beautybook.com", "Provider123!", ["ServiceProvider"]),
        ("salon.owner@beautybook.com", "Owner123!", ["SalonOwner"]),
    ];
    
}
