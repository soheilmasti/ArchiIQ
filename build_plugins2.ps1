$baseDir = "c:\Users\Soheil\Downloads\Jarvis\RevitCataloniaChecker"
$outDir = "c:\Users\Soheil\Downloads\Jarvis\ArchIQ_Multi"

function BuildPlugin2024() {
    $projDir = Join-Path $outDir "Plugin_2024"
    $csproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <LangVersion>12</LangVersion>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="System.Net.Http" Version="4.3.4" />
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
    <PackageReference Include="Revit_All_Main_Versions_API_x64" Version="2024.0.0" ExcludeAssets="runtime" />
    <Reference Include="PresentationCore" />
    <Reference Include="PresentationFramework" />
    <Reference Include="WindowsBase" />
    <Reference Include="System.Xaml" />
  </ItemGroup>
</Project>
"@
    Set-Content -Path (Join-Path $projDir "ArchIQ_2024.csproj") -Value $csproj -Encoding UTF8
    Write-Host "Re-Building Plugin 2024..."
    & "$env:USERPROFILE\.dotnet\dotnet.exe" build (Join-Path $projDir "ArchIQ_2024.csproj") -c Release
}

function BuildPlugin2026() {
    $projDir = Join-Path $outDir "Plugin_2026"
    Copy-Item -Path "$baseDir\*" -Destination $projDir -Recurse -Force
    Remove-Item -Path (Join-Path $projDir "bin") -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path (Join-Path $projDir "obj") -Recurse -Force -ErrorAction SilentlyContinue
    Rename-Item -Path (Join-Path $projDir "RevitCataloniaChecker.csproj") -NewName "ArchIQ_2026.csproj"

    $csproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <LangVersion>12</LangVersion>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <Reference Include="RevitAPI">
      <HintPath>C:\Program Files\Autodesk\Revit 2026\RevitAPI.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="RevitAPIUI">
      <HintPath>C:\Program Files\Autodesk\Revit 2026\RevitAPIUI.dll</HintPath>
      <Private>False</Private>
    </Reference>
  </ItemGroup>
</Project>
"@
    Set-Content -Path (Join-Path $projDir "ArchIQ_2026.csproj") -Value $csproj -Encoding UTF8
    Write-Host "Building Plugin 2026..."
    & "$env:USERPROFILE\.dotnet\dotnet.exe" build (Join-Path $projDir "ArchIQ_2026.csproj") -c Release
}

BuildPlugin2024
BuildPlugin2026

