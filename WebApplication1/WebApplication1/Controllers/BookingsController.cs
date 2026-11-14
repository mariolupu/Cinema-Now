using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Exceptions;
using Stripe.Checkout;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, ILogger<BookingsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Book(int id)
        {
            var screening = await _context.Screenings
                .Include(s => s.Movie)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (screening == null)
            {
                return NotFound();
            }

            var existingBookings = await _context.Bookings
                .Where(b => b.ScreeningId == id)
                .ToListAsync();

            var bookedSeats = new List<string>();
            foreach (var b in existingBookings)
            {
                if (!string.IsNullOrEmpty(b.SeatNumbers))
                {
                    var seats = b.SeatNumbers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var seat in seats)
                    {
                        bookedSeats.Add(seat.Trim());
                    }
                }
            }

            ViewBag.BookedSeats = bookedSeats;
            return View(screening);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int screeningId, string selectedSeats, string paymentMethod)
        {
            try
            {
                if (string.IsNullOrEmpty(selectedSeats))
                {
                    throw new CinemaException("Please select at least one seat!");
                }

                var screening = await _context.Screenings
                    .Include(s => s.Movie)
                    .FirstOrDefaultAsync(s => s.Id == screeningId);

                if (screening == null) return NotFound();

                var seatList = selectedSeats.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int seatCount = seatList.Length;
                decimal ticketPrice = 25.00m;
                decimal totalAmount = seatCount * ticketPrice;

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Challenge();

                if (paymentMethod == "Stripe")
                {
                    var domain = $"{Request.Scheme}://{Request.Host}";
                    long amountInBani = (long)(totalAmount * 100);

                    var options = new SessionCreateOptions
                    {
                        LineItems = new List<SessionLineItemOptions>
                        {
                            new SessionLineItemOptions
                            {
                                PriceData = new SessionLineItemPriceDataOptions
                                {
                                    UnitAmount = amountInBani,
                                    Currency = "ron",
                                    ProductData = new SessionLineItemPriceDataProductDataOptions
                                    {
                                        Name = $"Cinema Now - {screening.Movie?.Title}",
                                        Description = $"Hall: {screening.HallName} | Seats: {selectedSeats}"
                                    },
                                },
                                Quantity = 1,
                            },
                        },
                        Mode = "payment",
                        SuccessUrl = $"{domain}/Bookings/PaymentSuccess?screeningId={screeningId}&selectedSeats={Uri.EscapeDataString(selectedSeats)}&totalAmount={totalAmount}",
                        CancelUrl = $"{domain}/Bookings/Book/{screeningId}",
                    };

                    var service = new SessionService();
                    Session session = service.Create(options);

                    _logger.LogInformation("User {Email} initiated Stripe checkout for screening {ScreeningId}.", User.Identity?.Name, screeningId);
                    return Redirect(session.Url);
                }

                var booking = new Booking
                {
                    ScreeningId = screeningId,
                    UserId = userId,
                    SeatNumbers = selectedSeats,
                    BookingDate = DateTime.Now,
                    TotalAmount = totalAmount,
                    IsPaid = false,
                    PaymentMethod = "Cash"
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Email} successfully created a Cash booking (ID: {BookingId}) for screening {ScreeningId}.", User.Identity?.Name, booking.Id, screeningId);
                return RedirectToAction("Confirmation", new { id = booking.Id });
            }
            catch (SeatUnavailableException ex)
            {
                _logger.LogWarning(ex, "Seat error: {Message}", ex.Message);
                TempData["Error"] = ex.Message;
                return RedirectToAction("Book", new { id = screeningId });
            }
            catch (CinemaException ex)
            {
                _logger.LogWarning(ex, "Cinema business error: {Message}", ex.Message);
                TempData["Error"] = ex.Message;
                return RedirectToAction("Book", new { id = screeningId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during booking.");
                TempData["Error"] = "An unexpected error occurred. Please try again.";
                return RedirectToAction("Book", new { id = screeningId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCheckoutSession(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Screening)
                .ThenInclude(s => s.Movie)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null || booking.IsPaid)
            {
                return NotFound();
            }

            var domain = $"{Request.Scheme}://{Request.Host}";
            long amountInBani = (long)(booking.TotalAmount * 100);

            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = amountInBani,
                            Currency = "ron",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Cinema Now - {booking.Screening?.Movie?.Title}",
                                Description = $"Hall: {booking.Screening?.HallName} | Seats: {booking.SeatNumbers}"
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = $"{domain}/Bookings/PaymentSuccess?screeningId={booking.ScreeningId}&selectedSeats={Uri.EscapeDataString(booking.SeatNumbers)}&totalAmount={booking.TotalAmount}",
                CancelUrl = $"{domain}/Bookings/MyBookings",
            };

            var service = new SessionService();
            Session session = service.Create(options);

            return Redirect(session.Url);
        }

        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);

            var userBookings = await _context.Bookings
                .Include(b => b.Screening)
                .ThenInclude(s => s.Movie)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(userBookings);
        }

        [Authorize]
        public async Task<IActionResult> PaymentSuccess(int screeningId, string selectedSeats, decimal totalAmount)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(selectedSeats))
            {
                return RedirectToAction(nameof(MyBookings));
            }

            bool alreadyExists = await _context.Bookings.AnyAsync(b =>
                b.ScreeningId == screeningId &&
                b.UserId == userId &&
                b.SeatNumbers == selectedSeats &&
                b.IsPaid == true);

            if (!alreadyExists)
            {
                var booking = new Booking
                {
                    ScreeningId = screeningId,
                    UserId = userId,
                    SeatNumbers = selectedSeats,
                    BookingDate = DateTime.Now,
                    TotalAmount = totalAmount > 0 ? totalAmount : 25.00m,
                    IsPaid = true,
                    PaymentMethod = "Stripe"
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Email} completed a Stripe payment and created booking (ID: {BookingId}).", User.Identity?.Name, booking.Id);
            }

            return RedirectToAction(nameof(MyBookings));
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Screening)
                .ThenInclude(s => s.Movie)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            return View(booking);
        }
    }
}