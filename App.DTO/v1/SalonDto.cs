namespace App.DTO.v1;

public class SalonResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Address { get; set; } = default!;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public List<SalonStaffResponse> StaffMembers { get; set; } = new();
}

public class SalonStaffResponse
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = default!;
    public string Role { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
}
