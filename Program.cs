using System.Numerics;

namespace ETOPO2v2gParser;

public class Program
{
    public static void Main(string[] args)
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
        //    a) Query by lat/lon
        double testLat = 40.0;
        double testLon = -75.0;
        var (r, c) = LatLonToRowCol(testLat, testLon, header);

        float elev = elevations[r, c];
        float elevNorm = Normalize(elev, (float)header.MinValue, (float)header.MaxValue);

        Console.WriteLine($"Lookup at lat={testLat:F2}, lon={testLon:F2} => row={r}, col={c}, elevation={elev} m, normalized={elevNorm:F3}");

        //    b) Example centroid (x, y, z)
        //       Convert to lat/lon, then look up
        var centroid = new Vector3(0.5f, 0.5f, 0.7f);
        var (latDeg, lonDeg) = CartesianToLatLon(centroid.X, centroid.Y, centroid.Z);

        var (r2, c2) = LatLonToRowCol(latDeg, lonDeg, header);
        float elev2 = elevations[r2, c2];
        float elev2Norm = Normalize(elev2, (float)header.MinValue, (float)header.MaxValue);

        Console.WriteLine($"Centroid {centroid} => lat={latDeg:F2}, lon={lonDeg:F2}, row={r2}, col={c2}, elev={elev2} m, norm={elev2Norm:F3}");

        Console.WriteLine("\nDone!");
    }

    /// <summary>
    /// Parses the ESRI-style header file (.hdr).
    /// Example lines:
    ///   NCOLS 10801
    ///   NROWS 5401
    ///   XLLCENTER -180.00000
    ///   YLLCENTER -90.00000
    ///   CELLSIZE 0.03333333333
    ///   NODATA_VALUE 999999
    ///   BYTEORDER LSBFIRST
    ///   NUMBERTYPE 4_BYTE_FLOAT
    ///   MIN_VALUE -10722.0
    ///   MAX_VALUE 8046.0
    /// </summary>
    private static EsriHeader ParseHdrFile(string hdrPath)
    {
        var hdr = new EsriHeader();
        foreach (var line in File.ReadLines(hdrPath))
        {
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

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
    /// Reads the little-endian 4-byte float data (.flt) into a 2D float array.
    /// According to the header, row=0 should correspond to YLLCENTER in lat,
    /// and col=0 should correspond to XLLCENTER in lon.
    /// Dimensions: [NROWS, NCOLS]
    /// </summary>
    private static float[,] ReadFltFile(string fltPath, EsriHeader hdr)
    {
        float[,] data = new float[hdr.NRows, hdr.NCols];

        long expectedSize = (long)hdr.NRows * hdr.NCols * sizeof(float);
        long actualSize = new FileInfo(fltPath).Length;
        if (actualSize != expectedSize)
        {
            throw new IOException($"File size mismatch: expected {expectedSize} bytes, got {actualSize}");
        }

        using var fs = new FileStream(fltPath, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        // Row 0 in the file corresponds to the bottom row (YLLCENTER).
        // We'll store it exactly that way: data[0,*] = bottom row in lat.
        // If you want row=0 to be top latitude, invert your indexing logic here.
        for (int row = 0; row < hdr.NRows; row++)
        {
            for (int col = 0; col < hdr.NCols; col++)
            {
                // Because it's LSBFIRST (little-endian), we can just ReadSingle().
                float val = br.ReadSingle();
                data[row, col] = val;
            }
        }

        return data;
    }

    /// <summary>
    /// Converts (lat, lon) in degrees -> the nearest row/col in the float array.
    /// row 0 => YLLCENTER, col 0 => XLLCENTER.
    /// cellsize => 0.0333333 degrees (2 arc-min).
    /// </summary>
    private static (int row, int col) LatLonToRowCol(double lat, double lon, EsriHeader hdr)
    {
        // Since row=0 is yllcenter, row grows as we go north:
        double rowD = (lat - hdr.YllCenter) / hdr.CellSize;
        double colD = (lon - hdr.XllCenter) / hdr.CellSize;

        int row = (int)Math.Round(rowD);
        int col = (int)Math.Round(colD);

        row = Math.Clamp(row, 0, hdr.NRows - 1);
        col = Math.Clamp(col, 0, hdr.NCols - 1);

        return (row, col);
    }

    /// <summary>
    /// Converts a 3D cartesian point (x,y,z) on a sphere to (lat, lon) in degrees.
    /// Assumes z is "up" and radius is sqrt(x^2 + y^2 + z^2).
    /// </summary>
    private static (double latDeg, double lonDeg) CartesianToLatLon(double x, double y, double z)
    {
        double r = Math.Sqrt(x * x + y * y + z * z);
        if (r < 1e-9) return (0, 0);

        double lat = Math.Asin(z / r) * (180.0 / Math.PI);
        double lon = Math.Atan2(y, x) * (180.0 / Math.PI);
        return (lat, lon);
    }

    /// <summary>
    /// Normalizes a given elevation from [minVal, maxVal] to [-1, +1].
    /// Values outside the range get clamped to that scale.
    /// </summary>
    private static float Normalize(float elevation, float minVal, float maxVal)
    {
        if (Math.Abs(maxVal - minVal) < 1e-6f) return 0;
        float ratio = (elevation - minVal) / (maxVal - minVal); // 0..1
        float norm = (ratio * 2.0f) - 1.0f;                      // -1..+1
        return norm;
    }
}

/// <summary>
/// A small class to store ESRI .hdr info
/// </summary>
public class EsriHeader
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
