using Base.Domain;

namespace App.Domain;

public class BookingPayment : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public decimal Amount { get; set; }
}
