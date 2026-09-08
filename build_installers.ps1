$installerBaseDir = "c:\Users\Soheil\Downloads\Jarvis\ArchIQ_Installer"
$pluginBaseDir = "c:\Users\Soheil\Downloads\Jarvis\ArchIQ_Multi"
$desktopZip = "c:\Users\Soheil\Desktop\ArchIQ_Full_Package.zip"
$tempDir = "c:\Users\Soheil\Desktop\ArchIQ_Installers"

if (Test-Path $tempDir) { Remove-Item -Path $tempDir -Recurse -Force }
New-Item -Path $tempDir -ItemType Directory | Out-Null

foreach ($year in @("2024", "2025", "2026")) {
    $framework = if ($year -eq "2024") { "net48" } else { "net8.0-windows" }
    
    # Write .addin file
    $addin = @"
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>ArchIQ_$year</Name>
    <Assembly>ArchIQ_$year.dll</Assembly>
    <AddInId>$([guid]::NewGuid().ToString().ToUpper())</AddInId>
    <FullClassName>RevitCataloniaChecker.App</FullClassName>
    <VendorId>ArchIQ</VendorId>
    <VendorDescription>AI Architectural Code Checker ($year)</VendorDescription>
  </AddIn>
</RevitAddIns>
"@
    Set-Content -Path "$installerBaseDir\ArchIQ_${year}.addin" -Value $addin -Encoding UTF8
    
    # Copy DLLs
    Copy-Item -Path "$pluginBaseDir\Plugin_$year\bin\Release\$framework\ArchIQ_${year}.dll" -Destination "$installerBaseDir\ArchIQ_${year}.dll" -Force
    Copy-Item -Path "$pluginBaseDir\Plugin_$year\bin\Release\$framework\Newtonsoft.Json.dll" -Destination "$installerBaseDir\Newtonsoft.Json.dll" -Force
    
    # Update csproj
    $csproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>false</SelfContained>
    <AssemblyName>ArchIQ_Setup_$year</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <EmbeddedResource Include="ArchIQ_${year}.dll" />
    <EmbeddedResource Include="ArchIQ_${year}.addin" />
    <EmbeddedResource Include="Newtonsoft.Json.dll" />
  </ItemGroup>
</Project>
"@
    Set-Content -Path "$installerBaseDir\ArchIQ_Installer.csproj" -Value $csproj -Encoding UTF8
    
    # Update Program.cs
    $program = @"
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
            string targetDir = @"C:\ProgramData\Autodesk\Revit\Addins\$year";
            try
            {
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                Extract("ArchIQ_Installer.ArchIQ_${year}.dll", Path.Combine(targetDir, "ArchIQ_${year}.dll"));
                Extract("ArchIQ_Installer.ArchIQ_${year}.addin", Path.Combine(targetDir, "ArchIQ_${year}.addin"));
                Extract("ArchIQ_Installer.Newtonsoft.Json.dll", Path.Combine(targetDir, "Newtonsoft.Json.dll"));
                MessageBox.Show("ArchIQ AI Plugin for Revit $year installed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
"@
    Set-Content -Path "$installerBaseDir\Program.cs" -Value $program -Encoding UTF8
    
    Write-Host "Publishing Setup $year..."
    & "$env:USERPROFILE\.dotnet\dotnet.exe" publish "$installerBaseDir\ArchIQ_Installer.csproj" -c Release -r win-x64 --self-contained false -o $tempDir
}

Compress-Archive -Path "$tempDir\*.exe" -DestinationPath $desktopZip -Force
Remove-Item -Path $tempDir -Recurse -Force
