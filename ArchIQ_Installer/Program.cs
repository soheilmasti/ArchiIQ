using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ArchIQ_Installer
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string targetDir = @"C:\ProgramData\Autodesk\Revit\Addins\2026";
            try
            {
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                Extract("ArchIQ_Installer.ArchIQ_2026.dll", Path.Combine(targetDir, "ArchIQ_2026.dll"));
                Extract("ArchIQ_Installer.ArchIQ_2026.addin", Path.Combine(targetDir, "ArchIQ_2026.addin"));
                Extract("ArchIQ_Installer.Newtonsoft.Json.dll", Path.Combine(targetDir, "Newtonsoft.Json.dll"));
                MessageBox.Show("ArchIQ AI Plugin for Revit 2026 installed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        static void Extract(string res, string outPath)
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(res))
            {
                if (s == null) throw new Exception($"Missing {res}");
                using (FileStream fs = new FileStream(outPath, FileMode.Create)) s.CopyTo(fs);
            }
        }
    }
}
