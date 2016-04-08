using System;

namespace RaspberryCMS
{
    internal class WeatherStationMessage
    {
        public string DeviceId { get; set; }
        public DateTime PreciseTime { get; set; }
        public float Temperature { get; set; }
        public float Pressure { get; set; }
        public float Altitude { get; set; }
    }
}
