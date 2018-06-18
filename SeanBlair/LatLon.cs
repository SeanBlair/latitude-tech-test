namespace SeanBlair
{
    // The latitude and longitude values of a point on the surface of the earth.
    public struct LatLon
    {
        public double Lat { get; }
        public double Lon { get; }

        public LatLon(double lat, double lon)
        {
            if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
            {
                string errorMessage = $"Invalid LatLon value: [lat: {lat}, lon: {lon}]";
                throw new System.ArgumentException(errorMessage);
            }
            this.Lat = lat;
            this.Lon = lon;
        }
    }
}