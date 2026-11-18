namespace WebApplication1.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public int Duration { get; set; }
        public string? ImageUrl { get; set; }

        public string? Description { get; set; }

        public bool IsFeatured { get; set; } = false;


        public string? HomeSection { get; set; }
    }
}