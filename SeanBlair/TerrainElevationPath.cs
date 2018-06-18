using System;
using System.Collections.Generic;
using System.IO;

namespace SeanBlair
{
	// Given an array of LatLon structs that represents a flight path, and having the
	// correct .HGT files in the working directory, returns an array of integers 
	// representing the terrain elevation path in meters of the given flight path.
	public class TerrainElevationPath
	{
		const string HGTFileExtension = ".HGT";
		const int ArcsecondsPerDegree = 3600;
		const int ArcsecondsPerSample = 3;
		const int EntriesPerTileSide = 1201;
		const int BytesPerEntry = 2;
		const int HGTFileSize = EntriesPerTileSide * EntriesPerTileSide * BytesPerEntry;
		private LatLon[] flightPath;

		public TerrainElevationPath(LatLon[] flightPath)
		{
			this.flightPath = flightPath;
		}

		// Returns an array of integers representing the terrain elevation path in meters
		// that corresponds to the given flightPath LatLon array.
		// The .hgt tile files of side length 1201, that correspond to the flight path
		// area, are required in the working directory.
		// Given that the majority of consecutive LatLon readings in a flight path are in 
		// the same .hgt tile file, this algorithm collects all the consecutive LatLon 
		// structs that correspond to the same tile, and then opens that file once to read 
		// all the required elevation entries.
		public int[] GetElevationPath()
		{
			var elevationPath = new List<int>();
			if (flightPath.Length == 0)
			{
				return elevationPath.ToArray();
			}
			LatLon first = flightPath[0];
			var latLonsInSameTile = new List<LatLon>() { first };
			for (var i = 1; i < flightPath.Length; i++)
			{
				LatLon next = flightPath[i];
				if (IsInSameTile(first, next))
				{
					latLonsInSameTile.Add(next);
				}
				else
				{
					elevationPath.AddRange(GetElevationsFromTile(latLonsInSameTile));
					first = next;
					latLonsInSameTile = new List<LatLon>() { first };
				}
			}
			elevationPath.AddRange(GetElevationsFromTile(latLonsInSameTile));
			return elevationPath.ToArray();
		}

		// Returns a list of integers that represent terrain elevations in metres that
		// correspond to the given list of LatLons, all of which are found in the same 
		// .hgt tile file. Requires latLonsInSameTile to have at least one element.
		private IEnumerable<int> GetElevationsFromTile(List<LatLon> latLonsInSameTile)
		{
			string fileName = GetHGTFileNameLength7(latLonsInSameTile[0]);
			if (!File.Exists(fileName))
			{
				string hgtFileNameLength7 = fileName;
				fileName = GetHGTFileNameLength8(fileName);
				if (!File.Exists(fileName))
				{
					string workingDirectory = Directory.GetCurrentDirectory();
					string errorMessage = $"Error: Require a file named either " +
						$"{hgtFileNameLength7} or {fileName} in working" +
						$"directory: {workingDirectory}";
					throw new FileNotFoundException(errorMessage);
				}
			}
			return ReadElevations(fileName, latLonsInSameTile);
		}

		// Returns a list of elevations that correspond to the given LatLons by opening
		// an existing .hgt tile file and reading the corresponding entries.
		private IEnumerable<int> ReadElevations(string fileName, List<LatLon> latLonsInSameTile)
		{
			var elevations = new List<int>();
			byte[] entry;
			using (BinaryReader binReader = new BinaryReader(File.Open(fileName, FileMode.Open)))
			{
				if (binReader.BaseStream.Length != HGTFileSize)
				{
					string errorMessage = $"Error: The file with name {fileName} does not" +
						$" contain the required {HGTFileSize} bytes.";
					throw new ArgumentException(errorMessage);
				}
				foreach (var latLon in latLonsInSameTile)
				{
					binReader.BaseStream.Position = GetElevationEntryOffset(latLon);
					entry = binReader.ReadBytes(BytesPerEntry);
					if (BitConverter.IsLittleEndian)
					{
						Array.Reverse(entry);
					}
					elevations.Add(BitConverter.ToInt16(entry, 0));
				}
			}
			return elevations;
		}

		// Returns the number of bytes between the beginning of an .hgt file and the 
		// elevation entry that corresponds to latLon.
		private int GetElevationEntryOffset(LatLon latLon)
		{
			double fractionOfDegreeFromBottomLeft = latLon.Lat - Math.Floor(latLon.Lat);
			double arcsecondsFromBottomLeft = fractionOfDegreeFromBottomLeft * ArcsecondsPerDegree;
			int rowOffset =
				EntriesPerTileSide - (int)Math.Round(arcsecondsFromBottomLeft / ArcsecondsPerSample);
			fractionOfDegreeFromBottomLeft = latLon.Lon - Math.Floor(latLon.Lon);
			arcsecondsFromBottomLeft = fractionOfDegreeFromBottomLeft * ArcsecondsPerDegree;
			int columnOffset = (int)Math.Round(arcsecondsFromBottomLeft / ArcsecondsPerSample);
			int offset = (((rowOffset - 1) * EntriesPerTileSide) + columnOffset) * BytesPerEntry;
			return offset;
		}

		// Given a LatLon, returns the name of the .hgt tile file where the corresponding terrain 
		// altitude entry is found. The returned file name contains either N or S, then two digits, 
		// then either E or W, then three digits and the extension .HGT
		private string GetHGTFileNameLength7(LatLon latLon)
		{
			// Since .hgt tile files are named for the bottom left lat lon, Math.Floor is used.
			string latitude = Math.Abs(Math.Floor(latLon.Lat)).ToString();
			string longitude = Math.Abs(Math.Floor(latLon.Lon)).ToString();
			// Handle edge case when lat is exactly 90.0, elevation will be in tile with lat 89. 
			if (latLon.Lat == 90.0) latitude = "89";
			string fileName = latLon.Lat >= 0 ? "N" : "S";
			if (latitude.Length == 1)
			{
				fileName += "0";
			}
			fileName += latitude;
			fileName += latLon.Lon >= 0 ? "E" : "W";
			// Handle edge case when lon is exactly 180, elevation will be in tile with lon 179.
			if (latLon.Lon == 180.0) longitude = "179";
			if (longitude.Length == 2)
			{
				fileName += "0";
			}
			else if (longitude.Length == 1)
			{
				fileName += "00";
			}
			fileName += longitude;
			fileName += HGTFileExtension;
			return fileName;
		}

		// Given a .hgt filename with two digits for latitude, returns a .hgt 
		// filename with three digits for latitude.
		private string GetHGTFileNameLength8(string hgtFileNameLength7)
		{
			return hgtFileNameLength7.Insert(1, "0");
		}

		// Returns true if LatLon a and LatLon b both share the same latitude and 
		// longitude degrees.
		private bool IsInSameTile(LatLon a, LatLon b)
		{
			var sameLat = Math.Floor(a.Lat) == Math.Floor(b.Lat);
			var sameLong = Math.Floor(a.Lon) == Math.Floor(b.Lon);
			return sameLat && sameLong;
		}
	}
}