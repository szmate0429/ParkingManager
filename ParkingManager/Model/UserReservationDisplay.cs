using System;
using System.Collections.Generic;
using System.Text;

namespace ParkingManager.Model
{
    public class UserReservationDisplay
    {
        public string Id { get; set; } = string.Empty;
        public string SpotCode { get; set; } = string.Empty;
        public string SpotType {  get; set; } = "STANDARD";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
