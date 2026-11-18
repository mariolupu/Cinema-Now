using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Promotion
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Badge / Type")]
        public string BadgeText { get; set; } 

        [Required]
        public string Title { get; set; }

        public string Subtitle { get; set; } 

        [Required]
        public string Description { get; set; } 

        [Display(Name = "Discount Percentage")]
        public int? DiscountPercentage { get; set; } 

        [Display(Name = "Fixed Price (RON)")]
        public decimal? FixedPrice { get; set; }

        [Display(Name = "Applicable Days")]
        public string ApplicableDays { get; set; } 

        [Required]
        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }
}