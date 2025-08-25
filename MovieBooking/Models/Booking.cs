namespace MovieBooking.Models
{
    public class Booking
    {
       
        public string Id { get; set; } = "";

        public string UserName { get; set; } = "";
        public string ShowId { get; set; } = "";
        public List<string> SeatIds { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
