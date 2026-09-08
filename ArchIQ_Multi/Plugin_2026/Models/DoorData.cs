namespace RevitCataloniaChecker.Models
{
    public class DoorData
    {
        public long ElementId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FamilyAndType { get; set; } = string.Empty;
        public double ClearWidthM { get; set; }
        public double HeightM { get; set; }
        public string FromRoom { get; set; } = string.Empty;
        public string ToRoom { get; set; } = string.Empty;
        public string FireRating { get; set; } = string.Empty;
        public string LevelName { get; set; } = string.Empty;
    }
}
