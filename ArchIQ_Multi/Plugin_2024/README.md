# Revit Catalonia & Spain Building Code Checker (AI-Powered)

An advanced Autodesk Revit 2026 add-in built on **C# (.NET 8)** and **WPF**. It automatically audits BIM models against **Spain's National Building Code (CTE)** and **Catalonia's Habitability Regulations (Decret 141/2012)**, augmented with **Gemini 2.5 Flash AI** for architectural recommendations.

---

## 🌟 Key Features

1. **Deterministic High-Speed Rule Engine (Zero-Latency):**
   - **Catalonia Decret 141/2012 (Habitability):**
     - Total Dwelling Useful Surface (≥ 36.00 m²).
     - Living Space Area (≥ 14.00 m² or ≥ 20.00 m² with kitchen).
     - Double Bedroom Area (≥ 8.00 m²).
     - Single Bedroom Area (≥ 6.00 m²).
     - Ceiling Clear Height (≥ 2.50 m in habitable rooms, ≥ 2.20 m in service/auxiliary spaces).
     - Natural Lighting Ratio (Glazing area ≥ 1/8 or 12.5% of room floor area).
   - **Spain CTE (DB-SI & DB-SUA):**
     - Evacuation route door clear passage width (≥ 0.80 m).
2. **AI Architectural Consultant (Gemini 2.5 Flash):**
   - Integrated with the Gemini API to analyze spatial relationships, suggest design modifications, and summarize permit readiness.
3. **Interactive Revit View Sync:**
   - Selecting any row in the compliance table immediately selects, highlights, and zooms to that exact element in Revit.
4. **Export Reports:**
   - One-click export to Markdown (`.md`) and Excel/CSV (`.csv`) for submission and project documentation.

---

## 📁 Project Structure

```text
RevitCataloniaChecker/
│
├── App.cs                           # Registers the 'Catalonia Code' Ribbon Tab & button
├── Command.cs                       # IExternalCommand entry point
├── RevitCataloniaChecker.csproj     # .NET 8 WPF project file configured for Revit 2026
├── RevitCataloniaChecker.addin      # Manifest file for Autodesk Revit
│
├── Models/
│   ├── RoomData.cs                  # Extracted room geometric parameters
│   ├── DoorData.cs                  # Extracted door dimensions & ratings
│   └── ComplianceItem.cs            # Audit violation data model & UI bindings
│
├── Rules/
│   ├── CataloniaRegulations.cs      # Decret 141/2012 thresholds & logic
│   └── CteRegulations.cs            # CTE DB-SI & DB-SUA thresholds
│
├── Engine/
│   └── ComplianceEngine.cs          # Multi-element evaluation aggregator
│
├── Services/
│   ├── RevitDataExtractor.cs        # Safe Revit API geometry queries
│   ├── RevitExternalEventHandler.cs # Modeless UI thread-safe element selection & zoom
│   └── GeminiAiService.cs           # Gemini 2.5 Flash REST client
│
└── Views/
    ├── MainWindow.xaml              # Modern dark WPF architectural dashboard
    └── MainWindow.xaml.cs           # View controller & event handling
```

---

## 🚀 How to Build & Install into Revit 2026

### Step 1: Build the Solution
Open the project in **Visual Studio 2022** (with .NET 8 desktop development workload) and build the solution in **Release** or **Debug** mode:
```bash
cd c:\Users\Soheil\Downloads\Jarvis\RevitCataloniaChecker
dotnet build -c Release
```

### Step 2: Install Add-in Manifest
Copy the `RevitCataloniaChecker.addin` file to Revit's Addins folder:
```powershell
Copy-Item "c:\Users\Soheil\Downloads\Jarvis\RevitCataloniaChecker\RevitCataloniaChecker.addin" "$env:APPDATA\Autodesk\Revit\Addins\2026\"
```

*(Note: Verify that the `<Assembly>` path inside `RevitCataloniaChecker.addin` points to the compiled `RevitCataloniaChecker.dll`).*

### Step 3: Launch Revit 2026
1. Open Autodesk Revit 2026.
2. When prompted: *"Catalonia Building Code Checker - Do you want to load this add-in?"*, select **Always Load**.
3. Open any architectural project.
4. Navigate to the new Ribbon tab: **Catalonia Code** -> Click **Check Code (Spain/Cat)**.
5. The interactive dashboard will appear!
