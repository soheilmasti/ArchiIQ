namespace RevitCataloniaChecker.Models
{
    public class BomData
    {
        public string Category { get; set; } = string.Empty;
        public string ElementName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double TotalArea { get; set; }
        public double TotalVolume { get; set; }
    }
}
