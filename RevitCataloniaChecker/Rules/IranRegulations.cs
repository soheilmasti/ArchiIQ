using System.Collections.Generic;
using RevitCataloniaChecker.Models;

namespace RevitCataloniaChecker.Rules
{
    public static class IranRegulations
    {
        public static List<ComplianceItem> EvaluateRoom(RoomData room, string province)
        {
            var results = new List<ComplianceItem>();
            string nameLower = room.Name.ToLowerInvariant();

            bool isServiceRoom = nameLower.Contains("bath") || nameLower.Contains("حمام") || nameLower.Contains("سرویس") ||
                                nameLower.Contains("corridor") || nameLower.Contains("راهرو") || nameLower.Contains("kitchen") || nameLower.Contains("آشپزخانه");

            bool isExemptSpace = nameLower.Contains("stair") || nameLower.Contains("پله") ||
                                nameLower.Contains("elevator") || nameLower.Contains("آسانسور") ||
                                nameLower.Contains("storage") || nameLower.Contains("انبار") ||
                                nameLower.Contains("duct") || nameLower.Contains("دکت") ||
                                nameLower.Contains("cafe") || nameLower.Contains("کافه") ||
                                nameLower.Contains("restaurant") || nameLower.Contains("رستوران") ||
                                nameLower.Contains("office") || nameLower.Contains("اداری") ||
                                nameLower.Contains("shop") || nameLower.Contains("مغازه") ||
                                nameLower.Contains("تجاری");

            // --- 1. Ceiling Height (مبحث 4 مقررات ملی ساختمان) ---
            double minHeightHabitable = 2.60;
            double minHeightService = 2.40;

            if (province == "هرمزگان")
            {
                minHeightHabitable = 2.80; // stricter requirement for southern climates
            }

            double requiredHeight = isServiceRoom ? minHeightService : minHeightHabitable;
            string roomTypeCategory = isServiceRoom ? "فضای خدماتی (Service)" : "فضای اقامت (Habitable)";

            if (room.HeightM > 0 && !isExemptSpace)
            {
                if (room.HeightM < requiredHeight - 0.02)
                {
                    results.Add(new ComplianceItem
                    {
                        ElementId = room.ElementId,
                        ElementType = "Room (اتاق)",
                        ElementIdentifier = $"{room.Name} ({room.Number})",
                        ParameterChecked = "ارتفاع مفید سقف (Ceiling Height)",
                        MeasuredValue = $"{room.HeightM:F2} m",
                        RequiredValue = $"≥ {requiredHeight:F2} m",
                        RegulationReference = province == "هرمزگان" ? "مبحث ۴ مقررات ملی + ضوابط بومی اقلیم هرمزگان" : "مبحث ۴ مقررات ملی - حداقل ارتفاع",
                        Status = ComplianceStatus.NonCompliant,
                        Message = $"ارتفاع سقف کمتر از حد مجاز برای {roomTypeCategory} است.",
                        AiSuggestion = $"افزایش ارتفاع سقف به میزان {(requiredHeight - room.HeightM):F2} متر جهت تامین مبحث 4."
                    });
                }
                else
                {
                    results.Add(new ComplianceItem
                    {
                        ElementId = room.ElementId,
                        ElementType = "Room (اتاق)",
                        ElementIdentifier = $"{room.Name} ({room.Number})",
                        ParameterChecked = "ارتفاع مفید سقف (Ceiling Height)",
                        MeasuredValue = $"{room.HeightM:F2} m",
                        RequiredValue = $"≥ {requiredHeight:F2} m",
                        RegulationReference = "مبحث ۴ مقررات ملی",
                        Status = ComplianceStatus.Compliant,
                        Message = "ارتفاع سقف مورد تایید است."
                    });
                }
            }

            // --- 2. Minimum Area (مبحث 4) ---
            if (nameLower.Contains("living") || nameLower.Contains("نشیمن") || nameLower.Contains("پذیرایی"))
            {
                double minArea = 12.0; 
                results.Add(new ComplianceItem
                {
                    ElementId = room.ElementId,
                    ElementType = "Room (اتاق)",
                    ElementIdentifier = $"{room.Name} ({room.Number})",
                    ParameterChecked = "مساحت نشیمن (Living Area)",
                    MeasuredValue = $"{room.AreaM2:F2} m²",
                    RequiredValue = $"≥ {minArea:F1} m²",
                    RegulationReference = "مبحث ۴ مقررات ملی - فضای اقامت",
                    Status = room.AreaM2 >= minArea ? ComplianceStatus.Compliant : ComplianceStatus.NonCompliant,
                    Message = room.AreaM2 >= minArea ? "مساحت نشیمن مورد تایید است." : "فضای نشیمن کوچکتر از 12 متر مربع است.",
                    AiSuggestion = room.AreaM2 < minArea ? "دیوارهای پیرامونی را گسترش دهید." : string.Empty
                });
            }
            else if (nameLower.Contains("bed") || nameLower.Contains("خواب"))
            {
                bool isMaster = nameLower.Contains("master") || nameLower.Contains("مستر") || nameLower.Contains("اصلی");
                double minBedArea = isMaster ? 12.0 : 6.0;

                results.Add(new ComplianceItem
                {
                    ElementId = room.ElementId,
                    ElementType = "Room (اتاق)",
                    ElementIdentifier = $"{room.Name} ({room.Number})",
                    ParameterChecked = "مساحت اتاق خواب (Bedroom Area)",
                    MeasuredValue = $"{room.AreaM2:F2} m²",
                    RequiredValue = $"≥ {minBedArea:F1} m²",
                    RegulationReference = "مبحث ۴ مقررات ملی - اتاق‌های خواب",
                    Status = room.AreaM2 >= minBedArea ? ComplianceStatus.Compliant : ComplianceStatus.NonCompliant,
                    Message = room.AreaM2 >= minBedArea ? "مساحت اتاق خواب مورد تایید است." : $"مساحت خواب کمتر از حداقل {minBedArea} متر مربع است.",
                    AiSuggestion = room.AreaM2 < minBedArea ? "متراژ اتاق را افزایش دهید یا کاربری آن را تغییر دهید." : string.Empty
                });
            }

            // --- 3. Natural Lighting (مبحث 4) ---
            if (!isServiceRoom && !isExemptSpace && room.AreaM2 > 0)
            {
                double ratio = province == "هرمزگان" ? (1.0 / 6.0) : (1.0 / 8.0);
                double requiredWindowGlazing = room.AreaM2 * ratio;
                bool hasSufficientLight = room.WindowAreaM2 >= requiredWindowGlazing;

                results.Add(new ComplianceItem
                {
                    ElementId = room.ElementId,
                    ElementType = "Room (اتاق)",
                    ElementIdentifier = $"{room.Name} ({room.Number})",
                    ParameterChecked = "سطح نورگیری طبیعی (Natural Light Ratio)",
                    MeasuredValue = $"{room.WindowAreaM2:F2} m²",
                    RequiredValue = $"≥ {requiredWindowGlazing:F2} m²",
                    RegulationReference = province == "هرمزگان" ? "مبحث ۴ - اقلیم هرمزگان (1/6)" : "مبحث ۴ مقررات ملی (1/8)",
                    Status = hasSufficientLight ? ComplianceStatus.Compliant : (room.WindowCount == 0 ? ComplianceStatus.NonCompliant : ComplianceStatus.Warning),
                    Message = hasSufficientLight ? "نورگیری طبیعی مورد تایید است." : "سطح شیشه پنجره کمتر از حد مجاز مبحث 4 است.",
                    AiSuggestion = !hasSufficientLight ? $"پنجره‌ها را به میزان {(requiredWindowGlazing - room.WindowAreaM2):F2} متر مربع بزرگتر کنید." : string.Empty
                });
            }

            return results;
        }

        public static List<ComplianceItem> EvaluateDoor(DoorData door, string province)
        {
            var results = new List<ComplianceItem>();
            string nameLower = door.Name.ToLowerInvariant();

            bool isMainEntrance = nameLower.Contains("entrance") || nameLower.Contains("ورودی") || nameLower.Contains("main") || nameLower.Contains("اصلی");
            bool isBathroomDoor = nameLower.Contains("bath") || nameLower.Contains("سرویس") || nameLower.Contains("حمام") || door.ToRoom.Contains("حمام") || door.ToRoom.Contains("bath");

            // --- 4. Door Width (مبحث 3 ایمنی حریق و مبحث 4) ---
            double minWidth = 0.90; 
            if (isMainEntrance) minWidth = 1.10; 
            if (isBathroomDoor) minWidth = 0.70; 

            if (door.ClearWidthM > 0)
            {
                if (door.ClearWidthM < minWidth - 0.02)
                {
                    results.Add(new ComplianceItem
                    {
                        ElementId = door.ElementId,
                        ElementType = "Door (در)",
                        ElementIdentifier = $"Door {door.Name} (Level: {door.LevelName})",
                        ParameterChecked = "عرض مفید عبور (Clear Width)",
                        MeasuredValue = $"{door.ClearWidthM:F2} m",
                        RequiredValue = $"≥ {minWidth:F2} m",
                        RegulationReference = "مبحث ۳ (ایمنی حریق) و مبحث ۴",
                        Status = ComplianceStatus.NonCompliant,
                        Message = $"عرض در کمتر از حداقل عرض عبور/فرار ({minWidth}m) است.",
                        AiSuggestion = $"در را با یک نمونه عریض‌تر تعویض کنید."
                    });
                }
                else
                {
                    results.Add(new ComplianceItem
                    {
                        ElementId = door.ElementId,
                        ElementType = "Door (در)",
                        ElementIdentifier = $"Door {door.Name} (Level: {door.LevelName})",
                        ParameterChecked = "عرض مفید عبور (Clear Width)",
                        MeasuredValue = $"{door.ClearWidthM:F2} m",
                        RequiredValue = $"≥ {minWidth:F2} m",
                        RegulationReference = "مبحث ۳ و مبحث ۴",
                        Status = ComplianceStatus.Compliant,
                        Message = "عرض در استاندارد و مورد تایید است."
                    });
                }
            }

            return results;
        }
    }
}
