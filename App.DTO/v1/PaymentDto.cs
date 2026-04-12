namespace App.DTO.v1;

public class PaymentResponse
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime? PaidAt { get; set; }
}

public class PaymentCreateRequest
{
    public List<Guid> BookingIds { get; set; } = new();
    public string PaymentMethod { get; set; } = default!;
}
