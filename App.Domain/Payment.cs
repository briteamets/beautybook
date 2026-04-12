using Base.Domain;

namespace App.Domain;

public class Payment : BaseEntity
{
    public const string StatusPending = "Pending";
    public const string StatusPaid = "Paid";
    public const string StatusFailed = "Failed";
    public const string StatusRefunded = "Refunded";

    public Guid UserId { get; set; }
    public Identity.AppUser? User { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Status { get; set; } = StatusPending;
    public string PaymentMethod { get; set; } = default!;
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime From { get; set; }
    public DateTime? To { get; set; }

    public ICollection<BookingPayment>? BookingPayments { get; set; }
}
