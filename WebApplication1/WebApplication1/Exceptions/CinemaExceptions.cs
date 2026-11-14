using System;

namespace WebApplication1.Exceptions
{
    public class CinemaException : Exception
    {
        public CinemaException(string message) : base(message) { }
    }

    public class SeatUnavailableException : CinemaException
    {
        public SeatUnavailableException(string seatNumber)
            : base($"Seat {seatNumber} is already booked or unavailable.") { }
    }
}