namespace MovieBooking.Models
{
    public class Screen
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string TheaterId { get; set; } = "";
        public int Rows { get; set; } = 10;        // 10 rows by default
        public int SeatsPerRow { get; set; } = 10; // 10 seats per row by default
    }
}
