namespace App.DTO.v1;

public class ReviewResponse
{
    public Guid Id { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string UserName { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public class ReviewCreateRequest
{
    public Guid BookingId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
