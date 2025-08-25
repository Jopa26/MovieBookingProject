using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MovieBooking.Models;

namespace MovieBooking.UI
{
    /// <summary>
    /// Small, reusable helpers to keep ConsoleUI minimal and readable.
    /// Pure presentation/input utilities + a seat-map renderer + interactive booking.
    /// </summary>
    public static class ConsoleHelpers
    {
        // ---------- Input ----------

        public static string Prompt(string message)
        {
            Console.Write(message);
            return Console.ReadLine() ?? string.Empty;
        }

        /// <summary>Prompt for an int within [min, max]. Returns int.MinValue after N failed attempts.</summary>
        public static int PromptInt(string message, int min, int max, int retries = 3)
        {
            for (var attempt = 0; attempt < retries; attempt++)
            {
                Console.Write(message);
                var s = Console.ReadLine();
                if (int.TryParse(s, out var value) && value >= min && value <= max)
                    return value;
                Console.WriteLine($"Please enter a number between {min} and {max}.");
            }
            return int.MinValue;
        }

        /// <summary>Prompt a comma-separated seat list, normalize to uppercase, dedupe.</summary>
        public static List<string> PromptSeats(string message)
        {
            Console.Write(message);
            var raw = Console.ReadLine() ?? string.Empty;
            return NormalizeSeatList(raw);
        }

        private static List<string> NormalizeSeatList(string raw) =>
            (raw ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToUpperInvariant())
                .Distinct()
                .ToList();

        public static void Pause(string msg)
        {
            Console.WriteLine($"\n{msg}\nPress ENTER to continue...");
            Console.ReadLine();
        }

        public static string TitleCase(string s) =>
            CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());

        // ---------- Printing ----------

        public static void PrintMovieList(string heading, IList<Movie> movies)
        {
            Console.WriteLine($"\n{heading}:");
            for (var i = 0; i < movies.Count; i++)
                Console.WriteLine($"{i + 1}) {movies[i].Title}  ({movies[i].Genre}, {movies[i].Rating})");
        }

        public static void PrintShowList(string title, IList<Show> shows)
        {
            Console.WriteLine($"\nShowtimes for {TitleCase(title)}:");
            for (var i = 0; i < shows.Count; i++)
            {
                var when = shows[i].StartTime.ToString("ddd, MMM d · h:mm tt"); // e.g. Sat, Aug 24 · 3:41 PM
                Console.WriteLine($"{i + 1}) {when}");
            }
        }

        // ---------- Seat map ----------

        /// <summary>Renders seat map for a show using the BookingSystem for data access.</summary>
        public static void RenderSeatMap(BookingSystem system, string showId)
        {
            var (rows, cols, booked) = system.GetSeatStatus(showId);

            // Visual tuning
            const int rowLabelWidth = 3;      // " A "
            const int seatCellWidth = 3;      // "[ ]" or "[X]"
            int aisleAfter = cols >= 8 ? cols / 2 : -1; // add a gap after middle
            int gridWidth = rowLabelWidth + (cols * seatCellWidth) + (aisleAfter > 0 ? 2 : 0);

            // Screen banner
            Console.WriteLine();
            Console.WriteLine(new string('═', gridWidth));
            var screenText = "SCREEN";
            int padLeft = Math.Max(0, (gridWidth - screenText.Length) / 2);
            Console.WriteLine(new string(' ', padLeft) + screenText);
            Console.WriteLine(new string('═', gridWidth));
            Console.WriteLine("Legend: [ ] available   [X] booked\n");

            // Column headers
            Console.Write(new string(' ', rowLabelWidth));
            for (int c = 1; c <= cols; c++)
            {
                Console.Write($"{c,2} ");
                if (aisleAfter > 0 && c == aisleAfter) Console.Write("  ");
            }
            Console.WriteLine();

            // Rows
            for (int r = 0; r < rows; r++)
            {
                char rowChar = (char)('A' + r);
                Console.Write($" {rowChar} ");
                for (int c = 1; c <= cols; c++)
                {
                    string sid = $"{rowChar}{c}";
                    Console.Write(booked.Contains(sid) ? "[X]" : "[ ]");
                    if (aisleAfter > 0 && c == aisleAfter) Console.Write("  ");
                }
                Console.WriteLine();
            }

            // Availability summary
            var available = system.ListAvailableSeats(showId);
            Console.WriteLine($"\nAvailable seats: {available.Count}");
            if (available.Count > 0)
            {
                var sample = string.Join(", ", available.Take(10));
                Console.WriteLine($"Examples: {sample}{(available.Count > 10 ? ", ..." : "")}");
            }
        }

        public static void CancelBookingInteractive(BookingSystem system)
        {
            Console.WriteLine("\n— Cancel a Booking —");
            var id = Prompt("Enter Booking ID (blank to go back): ").Trim();
            if (string.IsNullOrWhiteSpace(id)) return;

            var ok = system.cancel_booking(id);
            Pause(ok ? " Booking canceled and seats freed."
                     : " Could not cancel — booking id not found.");
        }

        // ---------- Interactive booking (handles empty + taken seats gracefully) ----------

        /// <summary>
        /// Interactive booking loop.
        /// - Draws the seat map
        /// - Lets the user retry if input is blank or invalid
        /// - Cancels if Enter is pressed twice
        /// Returns the booking id if successful.
        /// </summary>
        public static string? BookSeatsInteractive(BookingSystem system, Show show)
        {
            while (true)
            {
                RenderSeatMap(system, show.Id);

                var seats = PromptSeats("\nEnter seats to book (comma, e.g., A1,A2 or blank to cancel): ");
                if (seats.Count == 0)
                {
                    Console.WriteLine("  No seats entered. Please try again (or press Enter again to cancel).");
                    var confirm = Prompt("Press Enter with no input again to cancel, or type seats: ").Trim();
                    if (string.IsNullOrWhiteSpace(confirm))
                        return null; // cancel booking
                    seats = (confirm ?? string.Empty)
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(s => s.ToUpperInvariant())
                            .Distinct()
                            .ToList();
                }

                if (system.TryBookSeats(show.Id, seats, out var bookingId))
                {
                    Pause($" Booking successful!\nBooking ID: {bookingId}");
                    return bookingId; // <-- IMPORTANT: return the id for later cancel
                }

                Console.WriteLine(" Seat(s) already booked or invalid. Try another seat.\n");
            }
        }
    }
}
