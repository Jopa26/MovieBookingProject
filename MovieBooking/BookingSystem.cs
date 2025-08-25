using System;
using System.Collections.Generic;
using System.Linq;
using MovieBooking.Models;

namespace MovieBooking
{
    // Simple in-memory service for movies, shows, and bookings.
    // Goal: keep it easy to follow and handle the core flow well.
    public class BookingSystem
    {
        // Short, readable booking ids like B001, B002...
        private int _bookingCounter = 0;

        // In-memory “tables”
        private readonly Dictionary<string, Movie> _movies = new();
        private readonly Dictionary<string, Theater> _theaters = new();
        private readonly Dictionary<string, Screen> _screens = new();
        private readonly Dictionary<string, Show> _shows = new();
        private readonly Dictionary<string, Booking> _bookings = new();

        // ---- Spec API (snake_case to match the prompt) ----
        public List<Movie> search_movies(string query) => SearchMovies(query);
        public List<Show> list_showtimes(string movie_title) => ListShowtimes(movie_title);
        public bool book_seats(string show_id, List<string> seats) => TryBookSeats(show_id, seats, out _);
        public bool cancel_booking(string booking_id) => CancelBooking(booking_id);

        // ---- Lookups ----

        // Case-insensitive title search; blank = return all (sorted).
        public List<Movie> SearchMovies(string? query)
        {
            query = (query ?? "").Trim();
            var src = string.IsNullOrEmpty(query)
                ? _movies.Values
                : _movies.Values.Where(m => m.Title.Contains(query, StringComparison.OrdinalIgnoreCase));
            return src.OrderBy(m => m.Title).ToList();
        }

        // Try exact title first, then partial. Always return a list (never null).
        public List<Show> ListShowtimes(string? movieTitle)
        {
            if (string.IsNullOrWhiteSpace(movieTitle)) return new();

            movieTitle = movieTitle.Trim();

            var movie =
                _movies.Values.FirstOrDefault(m =>
                    string.Equals(m.Title, movieTitle, StringComparison.OrdinalIgnoreCase))
                ?? _movies.Values.FirstOrDefault(m =>
                    m.Title.Contains(movieTitle, StringComparison.OrdinalIgnoreCase));

            if (movie is null) return new();

            return _shows.Values
                         .Where(s => s.MovieId == movie.Id)
                         .OrderBy(s => s.StartTime)
                         .ToList();
        }

        // ---- Booking ----

        // Single path for booking: all-or-nothing.
        // If any seat is invalid or taken, fail without changing state.
        // On success, returns a short booking id via out param.
        public bool TryBookSeats(string showId, List<string>? seats, out string? bookingId, string userName = "Guest")
        {
            bookingId = null;
            if (string.IsNullOrWhiteSpace(showId)) return false;
            if (seats is null || seats.Count == 0) return false;
            if (!_shows.TryGetValue(showId, out var show)) return false;

            var normalized = NormalizeSeats(seats);
            if (normalized.Count == 0) return false;

            // Lock per show to prevent double-booking during concurrent calls.
            lock (show)
            {
                if (!_screens.TryGetValue(show.ScreenId, out var screen)) return false;

                // Validate everything before we mutate.
                foreach (var seat in normalized)
                {
                    if (!SeatExistsOnScreen(screen, seat)) return false; // e.g., "Z99"
                    if (show.BookedSeatIds.Contains(seat)) return false; // already taken
                }

                // Commit seats and create the booking.
                foreach (var seat in normalized)
                    show.BookedSeatIds.Add(seat);

                var booking = new Booking
                {
                    UserName = userName,
                    ShowId = show.Id,
                    SeatIds = normalized,
                    CreatedAt = DateTime.UtcNow
                };

                _bookingCounter++;
                booking.Id = $"B{_bookingCounter:D3}"; // friendly id for the console

                _bookings[booking.Id] = booking;
                bookingId = booking.Id;
                return true;
            }
        }

        // Extra feature: free seats by booking id.
        public bool CancelBooking(string bookingId)
        {
            if (string.IsNullOrWhiteSpace(bookingId)) return false;
            if (!_bookings.TryGetValue(bookingId, out var booking)) return false;
            if (!_shows.TryGetValue(booking.ShowId, out var show)) return false;

            lock (show)
            {
                foreach (var seat in booking.SeatIds)
                    show.BookedSeatIds.Remove(seat);

                _bookings.Remove(bookingId);
                return true;
            }
        }

        // ---- Helpers used by the console UI ----

        // Enough info for the UI to draw a seat map.
        public (int rows, int cols, HashSet<string> booked) GetSeatStatus(string showId)
        {
            if (!_shows.TryGetValue(showId, out var show))
                throw new InvalidOperationException("Show not found");
            if (!_screens.TryGetValue(show.ScreenId, out var screen))
                throw new InvalidOperationException("Screen not found");

            return (screen.Rows, screen.SeatsPerRow, new HashSet<string>(show.BookedSeatIds));
        }

        // Convenience: list available seats for a show.
        public List<string> ListAvailableSeats(string showId)
        {
            if (!_shows.TryGetValue(showId, out var show)) return new();
            if (!_screens.TryGetValue(show.ScreenId, out var screen)) return new();

            return GenerateSeatIds(screen.Rows, screen.SeatsPerRow)
                   .Where(s => !show.BookedSeatIds.Contains(s))
                   .ToList();
        }

        public static IEnumerable<string> GenerateSeatIds(int rows, int seatsPerRow)
        {
            for (int r = 0; r < rows; r++)
            {
                char rowChar = (char)('A' + r);
                for (int c = 1; c <= seatsPerRow; c++)
                    yield return $"{rowChar}{c}";
            }
        }

        // ---- Seeding (used by Program.cs) ----

        public Movie AddMovie(string title, string genre, int durationMinutes, string rating)
        { var m = new Movie { Title = title, Genre = genre, DurationMinutes = durationMinutes, Rating = rating }; _movies[m.Id] = m; return m; }

        public Theater AddTheater(string name, string location)
        { var t = new Theater { Name = name, Location = location }; _theaters[t.Id] = t; return t; }

        public Screen AddScreen(Theater theater, int rows = 10, int seatsPerRow = 10)
        { var s = new Screen { TheaterId = theater.Id, Rows = rows, SeatsPerRow = seatsPerRow }; _screens[s.Id] = s; return s; }

        public Show AddShow(Movie movie, Screen screen, DateTime startTime)
        { var show = new Show { MovieId = movie.Id, ScreenId = screen.Id, StartTime = startTime }; _shows[show.Id] = show; return show; }

        // ---- Small private helpers ----

        // "a1, A2 , a2" → ["A1","A2"]
        private static List<string> NormalizeSeats(IEnumerable<string> seats) =>
            seats.Where(s => !string.IsNullOrWhiteSpace(s))
                 .Select(s => s.Trim().ToUpperInvariant())
                 .Distinct()
                 .ToList();

        // Parse “A10” and check bounds against the screen.
        private static bool SeatExistsOnScreen(Screen screen, string seatId)
        {
            if (!TryParseSeat(seatId, out var r, out var c)) return false;
            return r >= 0 && r < screen.Rows && c >= 0 && c < screen.SeatsPerRow;
        }

        private static bool TryParseSeat(string seatId, out int row, out int col)
        {
            row = col = -1;
            if (string.IsNullOrWhiteSpace(seatId)) return false;

            seatId = seatId.Trim().ToUpperInvariant();
            if (!char.IsLetter(seatId[0])) return false;

            var digits = seatId[1..];
            if (digits.Length == 0 || !digits.All(char.IsDigit)) return false;
            if (!int.TryParse(digits, out var number)) return false;

            row = seatId[0] - 'A';
            col = number - 1;
            return true;
        }
    }
}
