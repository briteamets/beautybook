namespace App.DTO.v1;

public class ServiceCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string? Description { get; set; }
}
