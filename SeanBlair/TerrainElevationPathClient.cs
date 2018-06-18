using System;
using System.IO;

namespace SeanBlair
{
	// Console application that demonstrates the functionality of the TerrainElevationPath class.
	// Requires files N48W124.hgt and N49W124.hgt in the working directory.
	class TerrainElevationPathClient
	{
		// A container to store a LatLon object and the name of its location.
		private struct Location
		{
			public string Name { get; }
			public LatLon LatLon { get; }
			public Location(string name, LatLon latLon)
			{
				this.Name = name;
				this.LatLon = latLon;
			}
		}

		static void Main(string[] args)
		{
			var testLocations = new Location[] {
				new Location("Victoria Harbour", new LatLon(48.424236, -123.383191)),
				new Location("Victoria Downtown", new LatLon(48.431202, -123.355085)),
				new Location("Mount Douglas", new LatLon(48.493261, -123.344507)),
				new Location("Elk Lake", new LatLon(48.530120, -123.397746)),
				new Location("Mount Newton", new LatLon(48.612521, -123.443495)),
				new Location("Saanich Inlet", new LatLon(48.630709, -123.509374)),
				new Location("UBC Vancouver Campus", new LatLon(49.263011, -123.248966)),
				new Location("Downtown Vancouver", new LatLon(49.281924, -123.119978)),
				new Location("Cypress Mountain", new LatLon(49.393771, -123.218798)),
				new Location("Howe Sound", new LatLon(49.684024, -123.167445)),
				new Location("Squamish", new LatLon(49.700848, -123.156063))
			};
			var flightPath = new LatLon[testLocations.Length];
			for (var i = 0; i < testLocations.Length; i++)
			{
				flightPath[i] = testLocations[i].LatLon;
			}
			var terrainElevationPath = new TerrainElevationPath(flightPath);
			int[] elevationPath;
			try
			{
				elevationPath = terrainElevationPath.GetElevationPath();
			}
			catch (Exception e) when (e is FileNotFoundException || e is ArgumentException)
			{
				Console.WriteLine(e.Message);
				Console.WriteLine();
				Console.WriteLine(e.StackTrace);
				return;
			}

			// Print the result to console.
			for (var i = 0; i < testLocations.Length; i++)
			{
				var location = testLocations[i];
				Console.WriteLine($"Elevation at {location.Name} " +
					$"[{location.LatLon.Lat}, {location.LatLon.Lon}] is: " +
					$"{elevationPath[i]} meters.");
			}
		}
	}
}
