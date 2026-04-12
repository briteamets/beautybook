namespace App.DTO.v1;

public class ServiceResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = default!;
    public bool IsActive { get; set; }
}

public class ServiceCreateRequest
{
    public Guid BeautyProviderId { get; set; }
    public Guid ServiceCategoryId { get; set; }
    public Dictionary<string, string> Name { get; set; } = new();
    public Dictionary<string, string>? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ServiceUpdateRequest
{
    public Guid ServiceCategoryId { get; set; }
    public Dictionary<string, string> Name { get; set; } = new();
    public Dictionary<string, string>? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
}
