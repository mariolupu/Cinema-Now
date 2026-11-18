using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Screening
    {
        public int Id { get; set; }

        public int MovieId { get; set; }
        public Movie? Movie { get; set; }

        [Required]
        public DateTime Showtime { get; set; } 

        public string HallName { get; set; } = "Hall 1";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } = 5.00m; 
    }
}