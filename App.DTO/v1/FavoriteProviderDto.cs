namespace App.DTO.v1;

public class FavoriteProviderResponse
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string ProfileName { get; set; } = default!;
    public string Address { get; set; } = default!;
    public decimal AverageRating { get; set; }
}

public class FavoriteProviderCreateRequest
{
    public Guid BeautyProviderId { get; set; }
}
