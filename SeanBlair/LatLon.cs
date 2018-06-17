namespace SeanBlair
{
    public struct LatLon
    {
        public double lat { get; }
        public double lon { get; }

        public LatLon(double lat, double lon)
        {
            this.lat = lat;
            this.lon = lon;
        }
    }
}