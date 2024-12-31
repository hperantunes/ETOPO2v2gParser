using System.Numerics;

namespace ETOPO2v2gParser;

public static class Program
{
    private static readonly char[] separator = [' ', '\t'];

    public static void Main()
    {
        // 1) Paths to your .hdr and .flt files
        //    Adjust these as needed. For example, if you unzipped them into "Data" folder:
        string dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        string hdrPath = Path.Combine(dataDir, "ETOPO2v2g_f4_LSB.hdr");
        string fltPath = Path.Combine(dataDir, "ETOPO2v2g_f4_LSB.flt");

        // 2) Parse the .hdr file
        var header = ParseHdrFile(hdrPath);
        Console.WriteLine("HDR Info:");
        Console.WriteLine($"  NCOLS      = {header.NCols}");
        Console.WriteLine($"  NROWS      = {header.NRows}");
        Console.WriteLine($"  XLLCENTER  = {header.XllCenter}");
        Console.WriteLine($"  YLLCENTER  = {header.YllCenter}");
        Console.WriteLine($"  CELLSIZE   = {header.CellSize}");
        Console.WriteLine($"  NODATA_VAL = {header.NoDataValue}");
        Console.WriteLine($"  MIN_VALUE  = {header.MinValue}");
        Console.WriteLine($"  MAX_VALUE  = {header.MaxValue}");
        Console.WriteLine($"  BYTEORDER  = {header.ByteOrder} (LSBFIRST = Little Endian)");
        Console.WriteLine($"  NUMBERTYPE = {header.NumberType} (4_BYTE_FLOAT)\n");

        // 3) Read the .flt data into memory
        float[,] elevations = ReadFltFile(fltPath, header);
        Console.WriteLine("Loaded the .flt data into a float[,] array.\n");

        // 4) Demonstration:
        //    a) Test a known lat/lon => e.g. (40N, 75W)
        double testLat = 40.0;  // +40
        double testLon = -75.0; // -75

        var (row, col) = LatLonToRowCol(testLat, testLon, header);

        float elev = elevations[row, col];
        float elevNorm = Normalize(elev, (float)header.MinValue, (float)header.MaxValue);

        Console.WriteLine($"A: Lookup at lat={testLat:F2}, lon={testLon:F2} => row={row}, col={col}, elevation={elev} m, normalized={elevNorm:F3}");

        //    b) Example centroid (x, y, z)
        //       Convert to lat/lon, then look up
        var centroid = new Vector3(0.5f, 0.5f, 0.7f);
        var (latDeg, lonDeg) = CartesianToLatLon(centroid.X, centroid.Y, centroid.Z);

        var (r2, c2) = LatLonToRowCol(latDeg, lonDeg, header);
        float elev2 = elevations[r2, c2];
        float elev2Norm = Normalize(elev2, (float)header.MinValue, (float)header.MaxValue);

        Console.WriteLine($"B: Centroid {centroid} => lat={latDeg:F2}, lon={lonDeg:F2}, row={r2}, col={c2}, elev={elev2} m, norm={elev2Norm:F3}");

        Console.WriteLine("\nDone!");
    }

    /// <summary>
    /// Parses a simple ESRI .hdr file. Example keys:
    ///   NCOLS, NROWS, XLLCENTER, YLLCENTER, CELLSIZE, NODATA_VALUE, BYTEORDER, etc.
    /// </summary>
    private static EsriHeader ParseHdrFile(string path)
    {
        var hdr = new EsriHeader();
        foreach (var line in File.ReadLines(path))
        {
            var parts = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            string key = parts[0].ToUpperInvariant();
            string val = parts[1];

            switch (key)
            {
                case "NCOLS":
                    hdr.NCols = int.Parse(val);
                    break;
                case "NROWS":
                    hdr.NRows = int.Parse(val);
                    break;
                case "XLLCENTER":
                    hdr.XllCenter = double.Parse(val);
                    break;
                case "YLLCENTER":
                    hdr.YllCenter = double.Parse(val);
                    break;
                case "CELLSIZE":
                    hdr.CellSize = double.Parse(val);
                    break;
                case "NODATA_VALUE":
                    hdr.NoDataValue = double.Parse(val);
                    break;
                case "BYTEORDER":
                    hdr.ByteOrder = val;
                    break;
                case "NUMBERTYPE":
                    hdr.NumberType = val;
                    break;
                case "MIN_VALUE":
                    hdr.MinValue = double.Parse(val);
                    break;
                case "MAX_VALUE":
                    hdr.MaxValue = double.Parse(val);
                    break;
            }
        }
        return hdr;
    }

    /// <summary>
    /// Reads the float data in forward order. The first row read is "row=0" in the array.
    /// This means row=0 is actually +90°N if the file is top-down.
    /// </summary>
    private static float[,] ReadFltFile(string fltPath, EsriHeader hdr)
    {
        float[,] data = new float[hdr.NRows, hdr.NCols];
        long expectedBytes = (long)hdr.NRows * hdr.NCols * sizeof(float);
        long actualBytes = new FileInfo(fltPath).Length;
        if (actualBytes != expectedBytes)
        {
            throw new IOException($"Expected {expectedBytes} bytes but got {actualBytes}.");
        }

        using var fs = new FileStream(fltPath, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        for (int row = 0; row < hdr.NRows; row++)
        {
            for (int col = 0; col < hdr.NCols; col++)
            {
                data[row, col] = br.ReadSingle(); // Already LSB => no swap needed
            }
        }

        return data;
    }

    /// <summary>
    /// We read row=0 as the very first row in the file, which is actually +90°N (the top).
    /// So lat=+90 => row=0; lat=-90 => row=NRows-1.
    /// => row = (90 - lat) / CellSize
    /// </summary>
    private static (int row, int col) LatLonToRowCol(double lat, double lon, EsriHeader hdr)
    {
        // row formula for top-down data:
        //   row=0 => +90°, row=NRows-1 => -90°
        double rowDouble = (90.0 - lat) / hdr.CellSize;

        // col formula usually unchanged if col=0 => -180°, col=NCols-1 => +180° 
        double colDouble = (lon - hdr.XllCenter) / hdr.CellSize;

        int row = (int)Math.Round(rowDouble);
        int col = (int)Math.Round(colDouble);

        // clamp to valid array range
        row = Math.Clamp(row, 0, hdr.NRows - 1);
        col = Math.Clamp(col, 0, hdr.NCols - 1);

        return (row, col);
    }

    /// <summary>
    /// Converts a 3D cartesian point (x, y, z) on a sphere to (lat, lon) in degrees,
    /// assuming y is the "up" (north pole) axis, and radius = sqrt(x^2 + y^2 + z^2).
    /// Also flips longitude to fix the mirrored globe.
    /// </summary>
    private static (double latDeg, double lonDeg) CartesianToLatLon(double x, double y, double z)
    {
        double r = Math.Sqrt(x * x + y * y + z * z);
        if (r < 1e-9)
        {
            return (0, 0);
        }

        // y is "north pole"
        double lat = Math.Asin(y / r) * (180.0 / Math.PI);

        // Normally: lon = Math.Atan2(z, x), but that can produce a mirrored view
        // if your orientation expects the positive z-axis to be "east" or "west" in the opposite sense.
        // To fix the mirror, just negate the angle:
        double lon = -Math.Atan2(z, x) * (180.0 / Math.PI);

        // Optionally, you can also wrap lon to [-180..180] or [0..360] if desired
        // e.g., if (lon < -180) lon += 360; if (lon > 180) lon -= 360;

        return (lat, lon);
    }

    /// <summary>
    /// Normalizes a given elevation from [minVal, maxVal] to [-1, +1].
    /// Values outside the range get clamped to that scale.
    /// </summary>
    private static float Normalize(float elevation, float minVal, float maxVal)
    {
        if (Math.Abs(maxVal - minVal) < 1e-6f)
        {
            return 0;
        }
        float ratio = (elevation - minVal) / (maxVal - minVal); // 0..1
        float norm = (ratio * 2.0f) - 1.0f;                      // -1..+1
        return norm;
    }

    private class EsriHeader
    {
        public int NCols { get; set; }
        public int NRows { get; set; }
        public double XllCenter { get; set; }
        public double YllCenter { get; set; }
        public double CellSize { get; set; }
        public double NoDataValue { get; set; }
        public string ByteOrder { get; set; } = "";
        public string NumberType { get; set; } = "";
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
    }
}
