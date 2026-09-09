# ArchIQ - AI-Powered Architectural Consultant

ArchIQ is a comprehensive Revit plugin designed to serve as an intelligent, interactive architectural consultant directly within your BIM environment. Powered by advanced AI (RAG - Retrieval-Augmented Generation), ArchIQ automatically extracts your Revit project data, analyzes rooms and spaces, checks for regional code compliance (e.g., Catalonia, Iran), and provides a fully interactive AI Chat interface.

## 🚀 Features

- **Multi-Version Support**: Single-click installers for Revit 2024, 2025, and 2026.
- **RAG-Powered AI Consultant**: Maintains a memory of your specific project details, identifying spaces and evaluating rules logically.
- **Interactive Chat UI**: Ask follow-up questions about compliance issues directly inside Revit.
- **Automated Memory Storage**: Saves ArchIQ_Memory_[ProjectName].md to your local documents for transparency.

## 📂 Project Structure

- RevitCataloniaChecker/: The core source code for the plugin and AI logic.
- ArchIQ_Multi/: The multi-targeted projects compiled specifically for the Revit 2024, 2025, and 2026 APIs.
- ArchIQ_Installer/: The automated C#-based installer source code.
- uild_installers.ps1: The PowerShell script to compile and package all the single-file executables.

## 🛠️ How to Build
Run the uild_installers.ps1 PowerShell script. It will automatically compile the plugins and generate the final Setup executables for all Revit versions onto your Desktop.

---
*Created by Soheil Masti*

