namespace App.DTO.v1;

public class BeautyProviderResponse
{
    public Guid Id { get; set; }
    public string ProfileName { get; set; } = default!;
    public string? Bio { get; set; }
    public string Address { get; set; } = default!;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public List<AvailabilityResponse> TodayAvailability { get; set; } = new();
}

public class BeautyProviderCreateRequest
{
    public string ProfileName { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Address { get; set; } = default!;
}

public class BeautyProviderUpdateRequest
{
    public string ProfileName { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Address { get; set; } = default!;
}
