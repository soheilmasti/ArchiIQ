using System.Collections.Generic;
using System.Linq;
using RevitCataloniaChecker.Models;
using RevitCataloniaChecker.Rules;

namespace RevitCataloniaChecker.Engine
{
    public class ComplianceEngine
    {
        public List<ComplianceItem> RunFullAudit(List<RoomData> rooms, List<DoorData> doors, string country, string province)
        {
            var auditResults = new List<ComplianceItem>();

            if (country == "Spain")
            {
                // 1. Evaluate Total Dwelling Useful Area (Catalonia specific)
                double totalUsefulArea = rooms.Sum(r => r.AreaM2);
                if (rooms.Count > 0)
                {
                    bool dwellingComplies = totalUsefulArea >= 36.0;
                    auditResults.Add(new ComplianceItem
                    {
                        ElementId = 0,
                        ElementType = "Dwelling / Project",
                        ElementIdentifier = "Global Project / Model Total",
                        ParameterChecked = "Total Usable Area (Superfície Útil Total)",
                        MeasuredValue = $"{totalUsefulArea:F2} m²",
                        RequiredValue = "≥ 36.00 m²",
                        RegulationReference = "Decret 141/2012 Art. 2.1",
                        Status = dwellingComplies ? ComplianceStatus.Compliant : ComplianceStatus.NonCompliant,
                        Message = dwellingComplies 
                            ? "Total dwelling useful area exceeds the 36 m² statutory minimum." 
                            : "Total dwelling useful area is below the 36 m² threshold (allowed down to 20 m² only for single-space micro-apartments).",
                        AiSuggestion = dwellingComplies ? string.Empty : "Verify if this project qualifies as an existing building exemption or expand overall envelope."
                    });
                }

                // 2. Evaluate Individual Rooms (Spain)
                foreach (var room in rooms)
                {
                    auditResults.AddRange(CataloniaRegulations.EvaluateRoom(room));
                }

                // 3. Evaluate Doors (Spain)
                foreach (var door in doors)
                {
                    auditResults.AddRange(CteRegulations.EvaluateDoor(door));
                }
            }
            else if (country == "ایران (Iran)")
            {
                // Evaluate Rooms (Iran)
                foreach (var room in rooms)
                {
                    auditResults.AddRange(IranRegulations.EvaluateRoom(room, province));
                }

                // Evaluate Doors (Iran)
                foreach (var door in doors)
                {
                    auditResults.AddRange(IranRegulations.EvaluateDoor(door, province));
                }
            }

            return auditResults;
        }
    }
}
