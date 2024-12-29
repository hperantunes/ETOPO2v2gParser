namespace ETOPO2v2gParser;

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
