using System.Net;
using App.Contracts;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[Route("/api/v{version:apiVersion}/bookings")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class BookingsController : BaseApiController
{
    private readonly IBookingService _bookingService;
    private readonly ISlotGeneratorService _slotGeneratorService;

    public BookingsController(IBookingService bookingService, ISlotGeneratorService slotGeneratorService)
    {
        _bookingService = bookingService;
        _slotGeneratorService = slotGeneratorService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> GetBookings()
    {
        var bookings = await _bookingService.GetUserBookingsAsync(GetCurrentUserId());
        return Ok(bookings.Select(b => b.ToResponse()));
    }

    [HttpGet("my")]
    public Task<ActionResult<IEnumerable<BookingResponse>>> GetMyBookings() => GetBookings();

    [HttpGet("incoming")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ServiceProvider")]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> GetIncomingBookings()
    {
        var bookings = await _bookingService.GetProviderBookingsAsync(GetCurrentUserId());
        return Ok(bookings.Select(b =>
        {
            var r = b.ToResponse();
            r.ClientName = b.User?.Email;
            return r;
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingResponse>> GetBooking(Guid id)
    {
        var booking = await _bookingService.GetByIdAsync(id);
        if (booking == null) return NotFound();
        if (!IsCurrentUser(booking.UserId)) return Forbid();
        return Ok(booking.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponse>> PostBooking([FromBody] BookingCreateRequest request)
    {
        if (request.ProviderServiceId.HasValue)
            return await CreateDynamicBooking(request);

        try
        {
            var booking = await _bookingService.CreateBookingAsync(
                GetCurrentUserId(), request.ServiceId, request.AvailabilityId, request.SalonId, request.Notes);
            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id, version = "1.0" }, booking.ToResponse());
        }
        catch (ArgumentException ex) { return ApiError(HttpStatusCode.BadRequest, ex.Message); }
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<BookingResponse>> UpdateBooking(Guid id, [FromBody] BookingUpdateRequest request)
    {
        try
        {
            var booking = await _bookingService.UpdateNotesAsync(id, GetCurrentUserId(), request.Notes);
            return Ok(booking.ToResponse());
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<ActionResult<BookingResponse>> CancelBooking(Guid id, [FromBody] BookingCancelRequest? request)
    {
        try
        {
            var booking = await _bookingService.CancelBookingAsync(id, GetCurrentUserId(), request?.Reason);
            return Ok(booking.ToResponse());
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return ApiError(HttpStatusCode.BadRequest, ex.Message); }
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ServiceProvider")]
    public async Task<ActionResult<BookingResponse>> UpdateBookingStatus(Guid id, [FromBody] BookingStatusUpdateRequest request)
    {
        try
        {
            var booking = await _bookingService.UpdateStatusAsync(id, GetCurrentUserId(), request.Status, request.Reason);
            return Ok(booking.ToResponse());
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return ApiError(HttpStatusCode.BadRequest, ex.Message); }
    }

    private async Task<ActionResult<BookingResponse>> CreateDynamicBooking(BookingCreateRequest request)
    {
        if (!request.ProviderServiceId.HasValue || !request.Date.HasValue || !request.StartTime.HasValue)
            return ApiError(HttpStatusCode.BadRequest, "providerServiceId, date, and startTime are required.");

        if (request.Date.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            return ApiError(HttpStatusCode.BadRequest, "Cannot book in the past.");

        var slots = await _slotGeneratorService.GenerateSlotsAsync(request.ProviderServiceId.Value, request.Date.Value);
        var slot = slots.FirstOrDefault(s => s.StartTime == request.StartTime.Value);
        if (slot == null)
            return ApiError(HttpStatusCode.BadRequest, "Selected slot is not available.");

        try
        {
            var booking = await _bookingService.CreateDynamicBookingAsync(
                GetCurrentUserId(), request.ProviderServiceId.Value,
                request.Date.Value, slot.StartTime, slot.EndTime, request.Notes);
            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id, version = "1.0" }, booking.ToResponse());
        }
        catch (ArgumentException ex) { return ApiError(HttpStatusCode.BadRequest, ex.Message); }
    }
}
