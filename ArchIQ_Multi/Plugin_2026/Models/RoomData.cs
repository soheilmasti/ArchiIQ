namespace RevitCataloniaChecker.Models
{
    public class RoomData
    {
        public long ElementId { get; set; }
        public string Number { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public double AreaM2 { get; set; }
        public double PerimeterM { get; set; }
        public double HeightM { get; set; }
        public double VolumeM3 { get; set; }
        public double WindowAreaM2 { get; set; }
        public int WindowCount { get; set; }
        public int DoorCount { get; set; }
        public string LevelName { get; set; } = string.Empty;

        // Ratio of window area to floor area (Il·luminació)
        public double LightingRatio => AreaM2 > 0 ? (WindowAreaM2 / AreaM2) : 0;
    }
}
