using System;
using System.Collections.Generic;
using System.Linq;
using MovieBooking.Models;
using static MovieBooking.UI.ConsoleHelpers;

namespace MovieBooking.UI
{
    /// <summary>
    /// Console UI: browse/search movies → pick show → book seats.
    /// Presentation only; all logic sits in BookingSystem.
    /// </summary>
    public class ConsoleUI
    {
        private readonly BookingSystem _system;

        public ConsoleUI(BookingSystem system)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
        }

        public void Run()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine(" Movie Ticket Booking\n");
                Console.WriteLine("1) Book tickets");
                Console.WriteLine("2) Cancel a booking");
                Console.WriteLine("   (blank to exit)");

                var choice = ConsoleHelpers.Prompt("\nChoose an option: ").Trim();
                if (string.IsNullOrWhiteSpace(choice)) return;

                if (choice == "2")
                {
                    ConsoleHelpers.CancelBookingInteractive(_system);
                    continue;
                }

                // Default to booking flow
                var movie = SelectMovie();
                if (movie is null) continue;

                var show = SelectShow(movie);
                if (show is null) continue;

                // returns booking id or null on cancel
                _ = ConsoleHelpers.BookSeatsInteractive(_system, show);
            }
        }

        // ---------- Step 1: Movie selection (browse + search) ----------

        private Movie? SelectMovie()
        {
            var allMovies = _system.search_movies("") ?? new List<Movie>();
            if (allMovies.Count == 0)
            {
                Pause(" No movies are currently available.");
                return null;
            }

            PrintMovieList("Available movies", allMovies);

            while (true)
            {
                var input = Prompt("\nPick movie # or type to search (blank to exit): ").Trim();
                if (string.IsNullOrWhiteSpace(input)) return null;

                // Pick by number
                if (int.TryParse(input, out var idx) && idx >= 1 && idx <= allMovies.Count)
                    return allMovies[idx - 1];

                // Or search
                var matches = _system.search_movies(input) ?? new List<Movie>();
                if (matches.Count == 0)
                {
                    Console.WriteLine($" No movies matched '{input}'. Try again.");
                    continue;
                }

                PrintMovieList($"Matches for '{TitleCase(input)}'", matches);
                var pick = PromptInt("\nPick movie #:", 1, matches.Count);
                if (pick != int.MinValue) return matches[pick - 1];

                Console.WriteLine(" No valid selection. Try again.");
            }
        }

        // ---------- Step 2: Showtime selection ----------

        private Show? SelectShow(Movie movie)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          
        {
            var shows = _system.list_showtimes(movie.Title) ?? new List<Show>();
            if (shows.Count == 0)
            {
                Pause($" No showtimes for {movie.Title}.");
                return null;
            }

            while (true)
            {
                PrintShowList(movie.Title, shows);
                var sel = PromptInt("\nPick show # (blank to go back): ", 1, shows.Count);
                if (sel == int.MinValue) return null; // go back to movies
                return shows[sel - 1];
            }
        }

        // ---------- Step 3: Booking loop ----------

        private void BookSeatsLoop(Show show)
        {
            ConsoleHelpers.BookSeatsInteractive(_system, show);
        }
    }
}
