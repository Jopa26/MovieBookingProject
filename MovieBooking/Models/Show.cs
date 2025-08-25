using System;
using System.Collections.Generic;

namespace MovieBooking.Models
{
    public class Show
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string MovieId { get; set; } = "";
        public string ScreenId { get; set; } = "";
        public DateTime StartTime { get; set; }

        // Booked seats like "A1", "B3", etc.
        public HashSet<string> BookedSeatIds { get; } = new HashSet<string>();
    }
}
