using System;

namespace MovieBooking.Models
{
    public class Movie
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Genre { get; set; } = "";
        public int DurationMinutes { get; set; }
        public string Rating { get; set; } = "PG-13";
    }
}
