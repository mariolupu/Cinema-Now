using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public IdentityUser? User { get; set; }

        public int ScreeningId { get; set; }
        public Screening? Screening { get; set; }

        public string SeatNumbers { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; } = DateTime.Now;
        public bool IsPaid { get; set; } = false;
        public decimal TotalAmount { get; set; } = 0;

        public string? PaymentMethod { get; set; } = "Cash";
        public bool IsCancelled { get; set; } = false;

        public int StudentTickets { get; set; } = 0;
        public int ChildTickets { get; set; } = 0;
    }
}