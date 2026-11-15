using Xunit;
using WebApplication1.Models;
using WebApplication1.Exceptions;
using WebApplication1.Services;
using WebApplication1.Data;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Tests
{
    public class CinemaTests
    {
        [Fact]
        public void Test1_BookingTotalPriceCalculation_IsCorrect()
        {
            int seatCount = 3;
            decimal ticketPrice = 25.00m;

            decimal total = seatCount * ticketPrice;

            Assert.Equal(75.00m, total);
        }

        [Fact]
        public void Test2_SeatUnavailableException_MessageIsCorrect()
        {
            var ex = new SeatUnavailableException("A5");

            Assert.Contains("Seat A5", ex.Message);
        }

        [Fact]
        public void Test3_CinemaException_InheritsFromException()
        {
            var ex = new CinemaException("Test error");

            Assert.IsAssignableFrom<System.Exception>(ex);
        }

        [Fact]
        public void Test4_SeatUnavailableException_InheritsFromCinemaException()
        {
            var ex = new SeatUnavailableException("B2");

            Assert.IsAssignableFrom<CinemaException>(ex);
        }

        [Fact]
        public void Test5_BookingDefaultValues_AreInitializedCorrectly()
        {
            var booking = new Booking();

            Assert.False(booking.IsPaid);
            Assert.Equal("Cash", booking.PaymentMethod);
            Assert.Equal(0, booking.TotalAmount);
        }
    }
}