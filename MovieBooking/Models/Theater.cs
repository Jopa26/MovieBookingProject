namespace MovieBooking.Models
{
    public class Theater
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Location { get; set; } = "";
    }
}
