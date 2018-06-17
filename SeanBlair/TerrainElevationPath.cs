using System;
using System.Collections.Generic;
using System.IO;

namespace SeanBlair
{
    // Given an array of LatLon structs that represents a flight path, and having the correct 
    // .HGT files in the working directory, returns an array of integers representing the terrain
    // elevation path in meters of the given flight path.
    public class TerrainElevationPath
    {
        const string HGTFileExtension = ".HGT";
        const int ArcsecondsPerDegree = 3600;
        const int ArcsecondsPerSample = 3;
        const int EntriesPerTileSide = 1201;
        const int BytesPerEntry = 2;
        private LatLon[] flightPath;

        public TerrainElevationPath(LatLon[] flightPath)
        {
            this.flightPath = flightPath;
        }


        // Returns an array of integers representing the terrain elevation path in meters that 
        // corresponds to the given flightPath LatLon array.
        // Requires the .hgt tile files of side length 1201 that correspond to the flight path 
        // in the working directory.
        // Given that the majority of consecutive LatLon readings in a flight path are in the 
        // same .hgt tile file, the algorithm collects all the consecutive LatLon structs that 
        // correspond to the same tile and then opens that file to find those elevation readings.
        internal int[] GetElevationPath()
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
                if (isInSameTile(first, next))
                {
                    latLonsInSameTile.Add(next);
                }
                else
                {
                    elevationPath.AddRange(getElevationsFromTile(latLonsInSameTile));
                    first = next;
                    latLonsInSameTile = new List<LatLon>() { first };
                }
            }
            elevationPath.AddRange(getElevationsFromTile(latLonsInSameTile));
            return elevationPath.ToArray();
        }

        // Returns a list of integers that represent terrain elevations in metres that correspond 
        // to the given list of LatLons, all of which are found in the same .hgt tile file.
        private IEnumerable<int> getElevationsFromTile(List<LatLon> latLonsInSameTile)
        {
            string fileName = getHGTFileNameLength7(latLonsInSameTile[0]);
            if (!File.Exists(fileName))
            {
                string hgtFileNameLength7 = fileName;
                fileName = getHGTFileNameLength8(fileName);
                if (!File.Exists(fileName))
                {
                    string workingDirectory = Directory.GetCurrentDirectory();
                    string errorMessage = $"Error: Require a file named either " +
                        $"{hgtFileNameLength7} or {fileName} in working" +
                        $"directory: {workingDirectory}";
                    Console.WriteLine(errorMessage);
                    throw new FileNotFoundException(errorMessage);
                }
            }
            return readElevations(fileName, latLonsInSameTile);
        }

        // Returns a list of elevations that correspond to the given LatLons by opening an
        // existing .hgt tile file and reading all the correct entries.
        private IEnumerable<int> readElevations(string fileName, List<LatLon> latLonsInSameTile)
        {
            var elevations = new List<int>();
            byte[] entry;
            using (BinaryReader binReader = new BinaryReader(File.Open(fileName, FileMode.Open)))
            {
                foreach (var latLon in latLonsInSameTile)
                {
                    binReader.BaseStream.Position = getElevationEntryOffset(latLon);
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

        // Returns the number of bytes between the beginning of the .hgt file and the elevation
        // entry that corresponds to latLon.
        private long getElevationEntryOffset(LatLon latLon)
        {
            double fractionOfDegreeFromBottomLeft = latLon.lat - Math.Floor(latLon.lat);
            double arcsecondsFromBottomLeft = fractionOfDegreeFromBottomLeft * ArcsecondsPerDegree;
            int sampleRow = 
                EntriesPerTileSide - 
                (int)Math.Round(arcsecondsFromBottomLeft / ArcsecondsPerSample);
            fractionOfDegreeFromBottomLeft = latLon.lon - Math.Floor(latLon.lon);
            arcsecondsFromBottomLeft = fractionOfDegreeFromBottomLeft * ArcsecondsPerDegree;
            int sampleColumn = (int)Math.Round(arcsecondsFromBottomLeft / ArcsecondsPerSample);
            int offset = (((sampleRow - 1) * EntriesPerTileSide) + sampleColumn) * BytesPerEntry;
            return offset;
        }

        // Given a LatLon, returns the name of the .hgt tile file where the corresponding terrain 
        // altitude entry is found. The returned file name contains either N or S, then two digits, 
        // then either E or W, then three digits and the extension .HGT
        private string getHGTFileNameLength7(LatLon latLon)
        {
            string latitude = Math.Abs(Math.Floor(latLon.lat)).ToString();
            string longitude = Math.Abs(Math.Floor(latLon.lon)).ToString();
            string fileName = latLon.lat >= 0 ? "N" : "S";
            if (latitude.Length == 1)
            {
                fileName += "0";
            }
            fileName += latitude;
            fileName += latLon.lon >= 0 ? "E" : "W";
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

        // Given a .hgt filename with two digits for latitude, returns a .hgt filename with 
        // three digits for latitude.
        private string getHGTFileNameLength8(string hgtFileNameLength7)
        {
            return hgtFileNameLength7.Insert(1, "0");
        }

        // Returns true if LatLon a and LatLon b both share the same latitude and longitude degrees.
        private bool isInSameTile(LatLon a, LatLon b)
        {
            var sameLat = Math.Floor(a.lat) == Math.Floor(b.lat);
            var sameLong = Math.Floor(a.lon) == Math.Floor(b.lon);
            return sameLat && sameLong;
        }
    }
}