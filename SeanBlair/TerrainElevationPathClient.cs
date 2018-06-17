using System;

namespace SeanBlair
{
    class TerrainElevationPathClient
    {
        static void Main(string[] args)
        {
            var victoriaHarbour = new LatLon(48.424236, -123.383191);
            var vicDowntown = new LatLon(48.431202, -123.355085);
            var mountDoug = new LatLon(48.493261, -123.344507);
            var elkLake = new LatLon(48.530120, -123.397746);
            var mountNewton = new LatLon(48.612521, -123.443495);
            var saanichInlet = new LatLon(48.630709, -123.509374);
            var ubc = new LatLon(49.263011, -123.248966);
            var downtownVancouver = new LatLon(49.281924, -123.119978);
            var simonFraserUni = new LatLon(49.277937, -122.918611);
            var burrardInlet = new LatLon(49.284007, -122.835490);

            var flightPath = 
                new LatLon[] { victoriaHarbour, vicDowntown, mountDoug, elkLake, mountNewton,
                    saanichInlet, ubc, downtownVancouver, simonFraserUni, burrardInlet };
            var tep = new TerrainElevationPath(flightPath);
            int[] elevationPath = tep.GetElevationPath();
            for (var i = 0; i < flightPath.Length; i++)
            {
                var latLong = flightPath[i];
                Console.WriteLine($"Elevation at lat-long {latLong.lat}, " +
                    $"{latLong.lon} is: {elevationPath[i]}");
            }
        }
    }
}
