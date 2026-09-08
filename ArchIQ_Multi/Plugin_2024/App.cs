using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace RevitCataloniaChecker
{
    public class App : IExternalApplication
    {
        private const string TabName = "ArchIQ";
        private const string PanelName = "AI BIM Inspector";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                try
                {
                    application.CreateRibbonTab(TabName);
                }
                catch { }

                RibbonPanel panel = application.CreateRibbonPanel(TabName, PanelName);

                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                string iconPath = Path.Combine(Path.GetDirectoryName(assemblyPath), "icon.jpg");

                var buttonData = new PushButtonData(
                    "BtnCheckArchIQ",
                    "ArchIQ\nInspector",
                    assemblyPath,
                    "RevitCataloniaChecker.Command")
                {
                    ToolTip = "Verifies active model against National Building Codes (Iran / Spain).",
                    LongDescription = "Performs instant deterministic verification and provides AI-powered architectural compliance recommendations."
                };

                var btn = panel.AddItem(buttonData) as PushButton;

                if (File.Exists(iconPath) && btn != null)
                {
                    try
                    {
                        var uriImage = new Uri(iconPath, UriKind.Absolute);
                        BitmapImage largeImage = new BitmapImage(uriImage);
                        btn.LargeImage = largeImage;
                    }
                    catch { }
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Startup Error", ex.ToString());
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
