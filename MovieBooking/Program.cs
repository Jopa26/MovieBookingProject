using System;
using MovieBooking.UI;

namespace MovieBooking
{
    public static class Program
    {
        public static void Main()
        {
            var system = Seed();
            var ui = new ConsoleUI(system); 
            ui.Run();
        }

        private static BookingSystem Seed()
        {
            var sys = new BookingSystem();

            // Movies
            var inception = sys.AddMovie("Inception", "SciFi", 148, "PG-13");
            var up = sys.AddMovie("Up", "Animation", 96, "PG");

            // Theater + screen
            var t = sys.AddTheater("UCLA Bruin Theater", "Westwood, CA");
            var s1 = sys.AddScreen(t, rows: 10, seatsPerRow: 10);

            // Showtimes
            sys.AddShow(inception, s1, DateTime.Now.AddHours(2));
            sys.AddShow(inception, s1, DateTime.Now.AddHours(5));
            sys.AddShow(up, s1, DateTime.Now.AddHours(3));

            return sys;
        }
    }
}
