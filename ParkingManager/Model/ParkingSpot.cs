using System;
using System.Collections.Generic;
using System.Text;

namespace ParkingManager.Model
{
    public class ParkingSpot
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string SpotType { get; set; } = "STANDARD";
    }
}
