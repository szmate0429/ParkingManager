using System;
using System.Collections.Generic;
using System.Text;

namespace ParkingManager.Model
{
    public class Reservation
    {
        public string Id { get; set; } = string.Empty;
        public int ParkingSpotId { get; set; }
        public string RequesterEmail { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; }
    }
}
