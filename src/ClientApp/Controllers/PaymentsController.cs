using ClientApp.Filters;
using ClientApp.Services;
using ClientApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ClientApp.Controllers;

[ClientAuthorize]
public class PaymentsController : Controller
{
    private readonly PaymentsApiService _paymentsApiService;

    public PaymentsController(PaymentsApiService paymentsApiService)
    {
        _paymentsApiService = paymentsApiService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ICollection<App.DTO.v1.PaymentResponse> payments;
        try
        {
            payments = await _paymentsApiService.GetAsync();
        }
        catch (HttpRequestException)
        {
            payments = [];
        }

        return View(new PaymentsIndexViewModel
        {
            Payments = payments.Select(p => new PaymentListItemViewModel
            {
                Id = p.Id,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status,
                PaidAt = p.PaidAt
            }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(Guid bookingId, string paymentMethod)
    {
        try
        {
            await _paymentsApiService.PayAsync(bookingId, paymentMethod);
        }
        catch (HttpRequestException)
        {
            return RedirectToAction("Details", "Bookings", new { id = bookingId, error = "pay" });
        }

        return RedirectToAction("Details", "Bookings", new { id = bookingId });
    }
}
