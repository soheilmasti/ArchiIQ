using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitCataloniaChecker.Services
{
    public enum EventAction
    {
        HighlightAndZoom,
        ColorizeErrors,
        ClearOverride
    }

    public class RevitExternalEventHandler : IExternalEventHandler
    {
        public EventAction Action { get; set; } = EventAction.HighlightAndZoom;
        public long TargetElementId { get; set; } = 0;
        public List<long> ErrorElementIds { get; set; } = new List<long>();

        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null) return;
            Document doc = uidoc.Document;

            if (Action == EventAction.HighlightAndZoom && TargetElementId > 0)
            {
                ElementId elemId = new ElementId(TargetElementId);
                Element elem = doc.GetElement(elemId);
                if (elem != null)
                {
                    try
                    {
                        uidoc.Selection.SetElementIds(new List<ElementId> { elemId });
                        uidoc.ShowElements(elem);
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Selection Error", $"Could not highlight element {TargetElementId}: {ex.Message}");
                    }
                }
            }
            else if (Action == EventAction.ColorizeErrors)
            {
                using (Transaction t = new Transaction(doc, "Colorize Non-Compliant Elements"))
                {
                    t.Start();
                    Color red = new Color(255, 50, 50);
                    OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                    ogs.SetSurfaceForegroundPatternColor(red);

                    // We need a solid fill pattern id
                    FilteredElementCollector fillCollector = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement));
                    FillPatternElement solidFill = fillCollector.Cast<FillPatternElement>().FirstOrDefault(f => f.GetFillPattern().IsSolidFill);
                    
                    if (solidFill != null)
                    {
                        ogs.SetSurfaceForegroundPatternId(solidFill.Id);
                    }

                    foreach (var id in ErrorElementIds)
                    {
                        if (id <= 0) continue;
                        ElementId eId = new ElementId(id);
                        try
                        {
                            doc.ActiveView.SetElementOverrides(eId, ogs);
                        }
                        catch { }
                    }
                    t.Commit();
                }
            }
            else if (Action == EventAction.ClearOverride && TargetElementId > 0)
            {
                using (Transaction t = new Transaction(doc, "Clear Element Override"))
                {
                    t.Start();
                    ElementId eId = new ElementId(TargetElementId);
                    OverrideGraphicSettings ogs = new OverrideGraphicSettings(); // default clears overrides
                    try
                    {
                        doc.ActiveView.SetElementOverrides(eId, ogs);
                    }
                    catch { }
                    t.Commit();
                }
            }
        }

        public string GetName() => "RevitCataloniaChecker_Handler";
    }
}
