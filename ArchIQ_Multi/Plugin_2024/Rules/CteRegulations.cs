using System;
using System.Collections.Generic;
using RevitCataloniaChecker.Models;

namespace RevitCataloniaChecker.Rules
{
    /// <summary>
    /// Implements regulatory thresholds based on Spain's CTE (Código Técnico de la Edificación)
    /// Focuses on DB-SI (Seguridad en caso de Incendio) and DB-SUA (Seguridad de Utilización y Accesibilidad)
    /// </summary>
    public static class CteRegulations
    {
        public const double MinDoorClearWidthM = 0.80; // CTE DB-SI Table 4.1 (Evacuation route clear width)
        public const double AccessibleDoorClearWidthM = 0.80; // CTE DB-SUA 9 Table 1.1

        public static List<ComplianceItem> EvaluateDoor(DoorData door)
        {
            var results = new List<ComplianceItem>();

            if (door.ClearWidthM > 0)
            {
                bool complies = door.ClearWidthM >= MinDoorClearWidthM - 0.01;

                results.Add(new ComplianceItem
                {
                    ElementId = door.ElementId,
                    ElementType = "Door",
                    ElementIdentifier = $"{door.FamilyAndType} (ID: {door.ElementId})",
                    ParameterChecked = "Clear Passage Width (Paso Libre)",
                    MeasuredValue = $"{door.ClearWidthM:F2} m",
                    RequiredValue = $"≥ {MinDoorClearWidthM:F2} m",
                    RegulationReference = "CTE DB-SI 3 Tabla 4.1 / DB-SUA 9",
                    Status = complies ? ComplianceStatus.Compliant : ComplianceStatus.NonCompliant,
                    Message = complies ? "Door width complies with CTE evacuation standards." : "Door clear passage width is less than 0.80 m.",
                    AiSuggestion = complies ? string.Empty : $"Modify door family or type to provide at least 0.80 m clear passage width (current deficit: {(MinDoorClearWidthM - door.ClearWidthM):F2} m)."
                });
            }

            return results;
        }
    }
}
