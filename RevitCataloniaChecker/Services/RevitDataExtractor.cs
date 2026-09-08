using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RevitCataloniaChecker.Models;

namespace RevitCataloniaChecker.Services
{
    public class RevitDataExtractor
    {
        private const double SqFtToSqM = 0.09290304;
        private const double FtToM = 0.3048;

        public static List<RoomData> ExtractRooms(Document doc)
        {
            var roomList = new List<RoomData>();

            var collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .ToElements();

            // Also collect windows to associate with rooms
            var windows = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Windows)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();

            foreach (var elem in collector)
            {
                if (elem is not Room room || room.Area <= 0)
                    continue;

                double areaM2 = room.Area * SqFtToSqM;
                double perimeterM = room.Perimeter * FtToM;
                double heightM = room.UnboundedHeight * FtToM;
                double volumeM3 = room.Volume * 0.028316846592;

                // Find windows inside or adjacent to this room
                double totalWindowAreaM2 = 0;
                int windowCount = 0;

                foreach (var win in windows)
                {
                    bool belongsToRoom = false;
                    try
                    {
                        if (win.Room != null && win.Room.Id == room.Id) belongsToRoom = true;
                        else if (win.FromRoom != null && win.FromRoom.Id == room.Id) belongsToRoom = true;
                        else if (win.ToRoom != null && win.ToRoom.Id == room.Id) belongsToRoom = true;
                    }
                    catch
                    {
                        // Some windows might not have spatial associations
                    }

                    if (belongsToRoom)
                    {
                        windowCount++;
                        // Estimate window area from Width and Height parameters
                        double w = GetParamValue(win, BuiltInParameter.WINDOW_WIDTH, BuiltInParameter.GENERIC_WIDTH) * FtToM;
                        double h = GetParamValue(win, BuiltInParameter.WINDOW_HEIGHT, BuiltInParameter.GENERIC_HEIGHT) * FtToM;
                        if (w > 0 && h > 0)
                        {
                            totalWindowAreaM2 += (w * h);
                        }
                    }
                }

                roomList.Add(new RoomData
                {
                    ElementId = room.Id.Value,
                    Number = room.Number ?? string.Empty,
                    Name = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "Unnamed Room",
                    Department = room.get_Parameter(BuiltInParameter.ROOM_DEPARTMENT)?.AsString() ?? string.Empty,
                    AreaM2 = areaM2,
                    PerimeterM = perimeterM,
                    HeightM = heightM > 0 ? heightM : 2.50, // Default fallback if unbounded height is zero
                    VolumeM3 = volumeM3,
                    WindowAreaM2 = totalWindowAreaM2,
                    WindowCount = windowCount,
                    LevelName = room.Level?.Name ?? string.Empty
                });
            }

            return roomList;
        }

        public static List<DoorData> ExtractDoors(Document doc)
        {
            var doorList = new List<DoorData>();

            var doors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Doors)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();

            foreach (var door in doors)
            {
                double widthFt = GetParamValue(door, BuiltInParameter.DOOR_WIDTH, BuiltInParameter.GENERIC_WIDTH);
                double heightFt = GetParamValue(door, BuiltInParameter.DOOR_HEIGHT, BuiltInParameter.GENERIC_HEIGHT);

                string fireRating = door.Symbol.get_Parameter(BuiltInParameter.FIRE_RATING)?.AsString() 
                    ?? door.get_Parameter(BuiltInParameter.FIRE_RATING)?.AsString() 
                    ?? "None";

                doorList.Add(new DoorData
                {
                    ElementId = door.Id.Value,
                    Name = door.Name,
                    FamilyAndType = $"{door.Symbol.FamilyName}: {door.Symbol.Name}",
                    ClearWidthM = widthFt * FtToM,
                    HeightM = heightFt * FtToM,
                    FromRoom = door.FromRoom?.Name ?? string.Empty,
                    ToRoom = door.ToRoom?.Name ?? string.Empty,
                    FireRating = fireRating,
                    LevelName = door.Document.GetElement(door.LevelId)?.Name ?? string.Empty
                });
            }

            return doorList;
        }

                public static List<BomData> ExtractBom(Document doc)
        {
            var dict = new Dictionary<string, BomData>();

            BuiltInCategory[] cats = {
                BuiltInCategory.OST_Walls, BuiltInCategory.OST_Floors, BuiltInCategory.OST_Roofs,
                BuiltInCategory.OST_Doors, BuiltInCategory.OST_Windows, BuiltInCategory.OST_Furniture,
                BuiltInCategory.OST_PlumbingFixtures, BuiltInCategory.OST_LightingFixtures
            };

            var collector = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .WherePasses(new ElementMulticategoryFilter(cats));

            foreach (Element elem in collector)
            {
                string catName = elem.Category?.Name ?? "Unknown";
                string elemName = elem.Name;
                if (elem is FamilyInstance fi && fi.Symbol != null)
                {
                    elemName = fi.Symbol.FamilyName + " - " + fi.Symbol.Name;
                }

                string key = $"{catName}||{elemName}";
                if (!dict.ContainsKey(key))
                {
                    dict[key] = new BomData { Category = catName, ElementName = elemName, Count = 0, TotalArea = 0, TotalVolume = 0 };
                }
                dict[key].Count++;

                Parameter areaParam = elem.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
                if (areaParam != null && areaParam.HasValue) dict[key].TotalArea += (areaParam.AsDouble() * SqFtToSqM);

                Parameter volParam = elem.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED);
                if (volParam != null && volParam.HasValue) dict[key].TotalVolume += (volParam.AsDouble() * 0.028316846592);
            }
            return dict.Values.OrderBy(x => x.Category).ThenBy(x => x.ElementName).ToList();
        }

        private static double GetParamValue(FamilyInstance instance, BuiltInParameter bip1, BuiltInParameter bip2)
        {
            // First check instance parameter
            var p = instance.get_Parameter(bip1) ?? instance.get_Parameter(bip2);
            if (p != null && p.HasValue) return p.AsDouble();

            // Check type (symbol) parameter
            if (instance.Symbol != null)
            {
                p = instance.Symbol.get_Parameter(bip1) ?? instance.Symbol.get_Parameter(bip2);
                if (p != null && p.HasValue) return p.AsDouble();
            }

            return 0;
        }
    }
}


