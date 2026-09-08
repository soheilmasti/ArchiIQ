using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCataloniaChecker.Services;
using RevitCataloniaChecker.Views;

namespace RevitCataloniaChecker
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        private static MainWindow? _activeWindow;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                if (uidoc == null || uidoc.Document == null)
                {
                    TaskDialog.Show("Revit Catalonia Checker", "Please open a project document first.");
                    return Result.Cancelled;
                }

                // If window is already open, bring to front
                if (_activeWindow != null && _activeWindow.IsLoaded)
                {
                    _activeWindow.Activate();
                    _activeWindow.ScanModel();
                    return Result.Succeeded;
                }

                var eventHandler = new RevitExternalEventHandler();
                var externalEvent = ExternalEvent.Create(eventHandler);

                _activeWindow = new MainWindow(uidoc, externalEvent, eventHandler);
                _activeWindow.Closed += (s, e) => _activeWindow = null;
                _activeWindow.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Error Launching Plugin", ex.ToString());
                return Result.Failed;
            }
        }
    }
}
