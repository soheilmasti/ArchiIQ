using System;
using System.Collections.Generic;
using RevitCataloniaChecker.Models;

namespace RevitCataloniaChecker.Rules
{
    /// <summary>
    /// Implements regulatory thresholds based on Catalonia's Habitability Decree (Decret 141/2012)
    /// </summary>
    public static class CataloniaRegulations
    {
        public const double MinCeilingHeightHabitableM = 2.50;
        public const double MinCeilingHeightServiceM = 2.20;
        public const double MinLightingGlazingRatio = 0.125; // 1/8 of room area (12.5%)
        public const double MinVentilationRatio = 0.0416;     // 1/24 of room area

        public const double MinLivingRoomAreaM2 = 14.0;
        public const double MinLivingKitchenAreaM2 = 20.0;
        public const double MinDoubleBedroomAreaM2 = 8.0;
        public const double MinSingleBedroomAreaM2 = 6.0;
        public const double MinBathroomAreaM2 = 2.5;

        public static List<ComplianceItem> EvaluateRoom(RoomData room)
        {
            var results = new List<ComplianceItem>();
            string nameLower = room.Name.ToLowerInvariant();

            // 1. Ceiling Height Evaluation
            bool isServiceRoom = nameLower.Contains("bath") || nameLower.Contains("bany") || nameLower.Contains("aseo") ||
                                nameLower.Contains("toilet") || nameLower.Contains("corridor") || nameLower.Contains("pasillo") ||
                                nameLower.Contains("distribuidor") || nameLower.Contains("hall") || nameLower.Contains("cuina") ||
                                nameLower.Contains("kitchen") || nameLower.Contains("cocina");

            bool isExemptSpace = nameLower.Contains("stair") || nameLower.Contains("escala") || nameLower.Contains("escalera") ||
                                nameLower.Contains("elevator") || nameLower.Contains("ascensor") || nameLower.Contains("lift") ||
                                nameLower.Contains("shaft") || nameLower.Contains("patinejo") || nameLower.Contains("storage") ||
                                nameLower.Contains("traster") || nameLower.Contains("almacen") || nameLower.Contains("closet") ||
                                nameLower.Contains("duct") || nameLower.Contains("cafe") || nameLower.Contains("cafeteria") ||
                                nameLower.Contains("restaurant") || nameLower.Contains("retail") || nameLower.Contains("shop") ||
                                nameLower.Contains("office") || nameLower.Contains("oficina") || nameLower.Contains("bar") ||
                                nameLower.Contains("comercial") || nameLower.Contains("commercial") || nameLower.Contains("tienda");

            double requiredHeight = isServiceRoom ? MinCeilingHeightServiceM : MinCeilingHeightHabitableM;
            string roomTypeCategory = isServiceRoom ? "Service / Auxiliary Space" : "Habitable Space";

            if (room.HeightM > 0 && !isExemptSpace)
            {
                if (room.HeightM < requiredHeight - 0.02)
                {
                    results.Add(new ComplianceItem
                    {
                        ElementId = room.ElementId,
                        ElementType = "Room",
                        ElementIdentifier = $"{room.Name} ({room.Number})",
                        ParameterChecked = "Ceiling Height (Altura Lliure)",
                        MeasuredValue = $"{room.HeightM:F2} m",
                        RequiredValue = $"≥ {requiredHeight:F2} m",
                        RegulationReference = "Decret 141/2012 Art. 3.2",
                        Status = ComplianceStatus.NonCompliant,
                        Message = $"Ceiling height is below minimum for {roomTypeCategory}.",
                        AiSuggestion = $"Increase slab-to-ceiling distance by at least {(requiredHeight - room.HeightM):F2} m to comply."
                    });
                }
                else
                {
                    results.Add(new ComplianceItem
                    {
                        ElementId = room.ElementId,
                        ElementType = "Room",
                        ElementIdentifier = $"{room.Name} ({room.Number})",
                        ParameterChecked = "Ceiling Height (Altura Lliure)",
                        MeasuredValue = $"{room.HeightM:F2} m",
                        RequiredValue = $"≥ {requiredHeight:F2} m",
                        RegulationReference = "Decret 141/2012 Art. 3.2",
                        Status = ComplianceStatus.Compliant,
                        Message = "Height complies with Catalonia habitability decree."
                    });
                }
            }

            // 2. Minimum Area Evaluation
            if (nameLower.Contains("living") || nameLower.Contains("estar") || nameLower.Contains("menjador") || nameLower.Contains("salon"))
            {
                bool hasKitchen = nameLower.Contains("cuina") || nameLower.Contains("kitchen") || nameLower.Contains("cocina");
                double minArea = hasKitchen ? MinLivingKitchenAreaM2 : MinLivingRoomAreaM2;

                results.Add(new ComplianceItem
                {
                    ElementId = room.ElementId,
                    ElementType = "Room",
                    ElementIdentifier = $"{room.Name} ({room.Number})",
                    ParameterChecked = "Usable Floor Area (Superfície Útil)",
                    MeasuredValue = $"{room.AreaM2:F2} m²",
                    RequiredValue = $"≥ {minArea:F1} m²",
                    RegulationReference = "Decret 141/2012 Annex 1",
                    Status = room.AreaM2 >= minArea ? ComplianceStatus.Compliant : ComplianceStatus.NonCompliant,
                    Message = room.AreaM2 >= minArea ? "Living space area meets Catalonian standards." : $"Living space is undersized for a {(hasKitchen ? "Living-Kitchen combo" : "Living room")}.",
                    AiSuggestion = room.AreaM2 < minArea ? $"Expand room boundaries by at least {(minArea - room.AreaM2):F2} m²." : string.Empty
                });
            }
            else if (nameLower.Contains("double") || nameLower.Contains("matrimon") || nameLower.Contains("principal") || nameLower.Contains("master"))
            {
                results.Add(new ComplianceItem
                {
                    ElementId = room.ElementId,
                    ElementType = "Room",
                    ElementIdentifier = $"{room.Name} ({room.Number})",
                    ParameterChecked = "Double Bedroom Area (Habitació Doble)",
                    MeasuredValue = $"{room.AreaM2:F2} m²",
                    RequiredValue = $"≥ {MinDoubleBedroomAreaM2:F1} m²",
                    RegulationReference = "Decret 141/2012 Annex 1",
                    Status = room.AreaM2 >= MinDoubleBedroomAreaM2 ? ComplianceStatus.Compliant : ComplianceStatus.NonCompliant,
                    Message = room.AreaM2 >= MinDoubleBedroomAreaM2 ? "Double bedroom meets Catalonian area requirements." : "Double bedroom is below 8.0 m².",
                    AiSuggestion = room.AreaM2 < MinDoubleBedroomAreaM2 ? "Re-evaluate wall placement or convert to a single bedroom classification." : string.Empty
                });
            }
            else if (nameLower.Contains("bed") || nameLower.Contains("habitac") || nameLower.Contains("dormitor") || nameLower.Contains("single"))
            {
                results.Add(new ComplianceItem
                {
                    ElementId = room.ElementId,
                    ElementType = "Room",
                    ElementIdentifier = $"{room.Name} ({room.Number})",
                    ParameterChecked = "Single Bedroom Area (Habitació Individual)",
                    MeasuredValue = $"{room.AreaM2:F2} m²",
                    RequiredValue = $"≥ {MinSingleBedroomAreaM2:F1} m²",
                    RegulationReference = "Decret 141/2012 Annex 1",
                    Status = room.AreaM2 >= MinSingleBedroomAreaM2 ? ComplianceStatus.Compliant : ComplianceStatus.NonCompliant,
                    Message = room.AreaM2 >= MinSingleBedroomAreaM2 ? "Bedroom meets minimum individual area requirements." : "Bedroom is below 6.0 m² minimum threshold.",
                    AiSuggestion = room.AreaM2 < MinSingleBedroomAreaM2 ? "Increase room area or designate as storage/dressing room." : string.Empty
                });
            }

            // 3. Natural Lighting Evaluation (Il·luminació natural) for habitable rooms
            if (!isServiceRoom && !isExemptSpace && room.AreaM2 > 0)
            {
                double requiredWindowGlazing = room.AreaM2 * MinLightingGlazingRatio;
                bool hasSufficientLight = room.WindowAreaM2 >= requiredWindowGlazing;

                results.Add(new ComplianceItem
                {
                    ElementId = room.ElementId,
                    ElementType = "Room",
                    ElementIdentifier = $"{room.Name} ({room.Number})",
                    ParameterChecked = "Natural Lighting Ratio (Il·luminació 1/8)",
                    MeasuredValue = $"{room.WindowAreaM2:F2} m² ({(room.LightingRatio * 100):F1}%)",
                    RequiredValue = $"≥ {requiredWindowGlazing:F2} m² (12.5%)",
                    RegulationReference = "Decret 141/2012 Art. 3.3",
                    Status = hasSufficientLight ? ComplianceStatus.Compliant : (room.WindowCount == 0 ? ComplianceStatus.NonCompliant : ComplianceStatus.Warning),
                    Message = hasSufficientLight ? "Natural lighting surface complies with 1/8 ratio." : "Window glazing area is below the 1/8 floor area requirement for habitable spaces.",
                    AiSuggestion = !hasSufficientLight ? $"Add or enlarge windows to gain at least {(requiredWindowGlazing - room.WindowAreaM2):F2} m² of glazed area." : string.Empty
                });
            }

            return results;
        }
    }
}
