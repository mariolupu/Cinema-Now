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

        private async Task<(decimal BaseTicketPrice, decimal OriginalPrice, string? AppliedOffer)> GetBaseTicketPriceAsync(Screening screening)
        {
            decimal originalPrice = screening.Price > 0 ? screening.Price : 5.00m;
            decimal currentPrice = originalPrice;
            List<string> appliedDiscounts = new List<string>();

            if (screening.Showtime.Hour < 17)
            {
                currentPrice *= 0.80m;
                appliedDiscounts.Add("Matinee -20%");
            }

            string dayName = screening.Showtime.ToString("dddd", System.Globalization.CultureInfo.InvariantCulture);
            var activePromos = await _context.Promotions
                .Where(p => p.IsActive)
                .ToListAsync();

            var matchingPromo = activePromos.FirstOrDefault(p =>
                string.IsNullOrEmpty(p.ApplicableDays) ||
                p.ApplicableDays.Contains("All", StringComparison.OrdinalIgnoreCase) ||
                p.ApplicableDays.Contains(dayName, StringComparison.OrdinalIgnoreCase));

            if (matchingPromo != null)
            {
                if (matchingPromo.FixedPrice.HasValue && matchingPromo.FixedPrice.Value > 0)
                {
                    currentPrice = matchingPromo.FixedPrice.Value;
                    appliedDiscounts.Clear();
                    appliedDiscounts.Add($"{matchingPromo.Title} ({matchingPromo.FixedPrice.Value} RON)");
                }
                else if (matchingPromo.DiscountPercentage.HasValue && matchingPromo.DiscountPercentage.Value > 0)
                {
                    currentPrice = currentPrice * (1 - (matchingPromo.DiscountPercentage.Value / 100m));
                    appliedDiscounts.Add($"{matchingPromo.Title} (-{matchingPromo.DiscountPercentage.Value}%)");
                }
            }

            currentPrice = Math.Round(currentPrice, 2);
            string? offerSummary = appliedDiscounts.Any() ? string.Join(" + ", appliedDiscounts) : null;

            return (currentPrice, originalPrice, offerSummary);
        }

        public async Task<IActionResult> Book(int id)
        {
            var screening = await _context.Screenings
                .Include(s => s.Movie)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (screening == null) return NotFound();

            var now = DateTime.Now;
            var existingBookings = await _context.Bookings
                .Where(b => b.ScreeningId == id && !b.IsCancelled)
                .ToListAsync();

            var bookedSeats = new List<string>();
            foreach (var b in existingBookings)
            {
                bool isExpired = !b.IsPaid && now > screening.Showtime.AddMinutes(30);
                if (!isExpired && !string.IsNullOrEmpty(b.SeatNumbers))
                {
                    var seats = b.SeatNumbers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var seat in seats)
                    {
                        bookedSeats.Add(seat.Trim());
                    }
                }
            }

            var priceInfo = await GetBaseTicketPriceAsync(screening);
            ViewBag.TicketPrice = priceInfo.BaseTicketPrice;
            ViewBag.OriginalPrice = priceInfo.OriginalPrice;
            ViewBag.AppliedOffer = priceInfo.AppliedOffer;
            ViewBag.BookedSeats = bookedSeats;

            return View(screening);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int screeningId, string selectedSeats, string paymentMethod, int studentCount = 0, int childCount = 0)
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
                int totalSeats = seatList.Length;

                studentCount = Math.Max(0, studentCount);
                childCount = Math.Max(0, childCount);

                if (studentCount + childCount > totalSeats)
                {
                    throw new CinemaException($"The number of Student ({studentCount}) and Child ({childCount}) tickets cannot exceed total selected seats ({totalSeats}).");
                }

                int standardCount = totalSeats - studentCount - childCount;

                var priceInfo = await GetBaseTicketPriceAsync(screening);
                decimal unitPrice = priceInfo.BaseTicketPrice;

                decimal standardTotal = standardCount * unitPrice;
                decimal studentTotal = studentCount * (unitPrice * 0.75m); // -25%
                decimal childTotal = childCount * (unitPrice * 0.50m);     // -50%

                decimal totalAmount = Math.Round(standardTotal + studentTotal + childTotal, 2);

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
                                        Description = $"Hall: {screening.HallName} | Seats: {selectedSeats} (Std: {standardCount}, Student: {studentCount}, Child: {childCount})"
                                    },
                                },
                                Quantity = 1,
                            },
                        },
                        Mode = "payment",
                        SuccessUrl = $"{domain}/Bookings/PaymentSuccess?screeningId={screeningId}&selectedSeats={Uri.EscapeDataString(selectedSeats)}&totalAmount={totalAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}&studentCount={studentCount}&childCount={childCount}",
                        CancelUrl = $"{domain}/Bookings/Book/{screeningId}",
                    };

                    var service = new SessionService();
                    Session session = service.Create(options);

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
                    PaymentMethod = "Cash",
                    StudentTickets = studentCount,
                    ChildTickets = childCount
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                return RedirectToAction("Confirmation", new { id = booking.Id });
            }
            catch (CinemaException ex)
            {
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

            if (booking == null || booking.IsPaid) return NotFound();

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
                            Currency = "eur",
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
                SuccessUrl = $"{domain}/Bookings/PaymentSuccess?screeningId={booking.ScreeningId}&selectedSeats={Uri.EscapeDataString(booking.SeatNumbers)}&totalAmount={booking.TotalAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}&studentCount={booking.StudentTickets}&childCount={booking.ChildTickets}",
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings
                .Include(b => b.Screening)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (booking == null) return NotFound();

            if (booking.IsPaid)
            {
                TempData["Error"] = "Paid bookings cannot be cancelled online for refunds.";
                return RedirectToAction(nameof(MyBookings));
            }

            booking.IsCancelled = true;
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reservation successfully cancelled.";
            return RedirectToAction(nameof(MyBookings));
        }

        [Authorize]
        public async Task<IActionResult> PaymentSuccess(int screeningId, string selectedSeats, decimal totalAmount, int studentCount = 0, int childCount = 0)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(selectedSeats))
            {
                return RedirectToAction(nameof(MyBookings));
            }

            var existingPending = await _context.Bookings.FirstOrDefaultAsync(b =>
                b.ScreeningId == screeningId &&
                b.UserId == userId &&
                b.SeatNumbers == selectedSeats &&
                !b.IsPaid && !b.IsCancelled);

            if (existingPending != null)
            {
                existingPending.IsPaid = true;
                existingPending.PaymentMethod = "Stripe";
                _context.Bookings.Update(existingPending);
                await _context.SaveChangesAsync();
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
                    TotalAmount = totalAmount > 0 ? totalAmount : 5.00m,
                    IsPaid = true,
                    PaymentMethod = "Stripe",
                    StudentTickets = studentCount,
                    ChildTickets = childCount
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
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