using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCataloniaChecker.Engine;
using RevitCataloniaChecker.Models;
using RevitCataloniaChecker.Services;

namespace RevitCataloniaChecker.Views
{
    public partial class MainWindow : Window
    {
        private readonly UIDocument _uidoc;
        private readonly Document _doc;
        private readonly ExternalEvent _externalEvent;
        private readonly RevitExternalEventHandler _eventHandler;
        private readonly ComplianceEngine _engine;
        private readonly GeminiAiService _aiService;

        private List<RoomData> _rooms = new List<RoomData>();
        private List<DoorData> _doors = new List<DoorData>();
        private List<ComplianceItem> _allItems = new List<ComplianceItem>();

        public ObservableCollection<ChatMessage> ChatMessages { get; set; } = new ObservableCollection<ChatMessage>();

        public MainWindow(UIDocument uidoc, ExternalEvent externalEvent, RevitExternalEventHandler eventHandler)
        {
            InitializeComponent();
            _uidoc = uidoc;
            _doc = uidoc.Document;
            _externalEvent = externalEvent;
            _eventHandler = eventHandler;
            _engine = new ComplianceEngine();
            _aiService = new GeminiAiService();

            ChatList.ItemsSource = ChatMessages;
        }

        public void ScanModel()
        {
            try
            {
                _rooms = RevitDataExtractor.ExtractRooms(_doc);
                _doors = RevitDataExtractor.ExtractDoors(_doc);

                string selectedCountry = (CmbCountry.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Spain";
                string selectedProvince = (CmbProvince.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Catalonia";

                _allItems = _engine.RunFullAudit(_rooms, _doors, selectedCountry, selectedProvince);

                ApplyFilters();

                var errorIds = _allItems.Where(i => i.Status == ComplianceStatus.NonCompliant).Select(i => i.ElementId).ToList();
                if (errorIds.Any() && _externalEvent != null && _eventHandler != null)
                {
                    _eventHandler.Action = EventAction.ColorizeErrors;
                    _eventHandler.ErrorElementIds = errorIds;
                    _externalEvent.Raise();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error scanning model: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            var query = TxtSearch?.Text?.ToLowerInvariant() ?? string.Empty;
            string filterChoice = (CmbFilter?.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Show All";

            var filtered = _allItems.AsEnumerable();

            filtered = filtered.Where(i => !i.IsResolved);

            if (!string.IsNullOrWhiteSpace(query))
            {
                filtered = filtered.Where(i => 
                    i.ElementIdentifier.ToLowerInvariant().Contains(query) ||
                    i.ParameterChecked.ToLowerInvariant().Contains(query) ||
                    i.RegulationReference.ToLowerInvariant().Contains(query) ||
                    i.Message.ToLowerInvariant().Contains(query));
            }

            if (filterChoice == "Only Violations")
                filtered = filtered.Where(i => i.Status == ComplianceStatus.NonCompliant);
            else if (filterChoice == "Only Compliant")
                filtered = filtered.Where(i => i.Status == ComplianceStatus.Compliant);

            if (GridCompliance != null)
            {
                GridCompliance.ItemsSource = filtered.ToList();
            }
        }

        private void OnCountrySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbProvince == null) return;
            CmbProvince.Items.Clear();

            var selectedCountry = (CmbCountry.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (selectedCountry != null && selectedCountry.Contains("Iran"))
            {
                CmbProvince.Items.Add(new ComboBoxItem { Content = "Tehran", IsSelected = true });
                CmbProvince.Items.Add(new ComboBoxItem { Content = "Hormozgan" });
            }
            else
            {
                CmbProvince.Items.Add(new ComboBoxItem { Content = "Catalonia", IsSelected = true });
                CmbProvince.Items.Add(new ComboBoxItem { Content = "Madrid" });
            }
        }

        private void OnResolvedChecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is ComplianceItem item)
            {
                if (item.IsResolved && _externalEvent != null && _eventHandler != null)
                {
                    _eventHandler.Action = EventAction.ClearOverride;
                    _eventHandler.TargetElementId = item.ElementId;
                    _externalEvent.Raise();
                }
                ApplyFilters();
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void OnFilterSelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void OnRescanClicked(object sender, RoutedEventArgs e) => ScanModel();

        private void OnGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridCompliance.SelectedItem is ComplianceItem selected)
            {
                if (_externalEvent != null && _eventHandler != null)
                {
                    _eventHandler.Action = EventAction.HighlightAndZoom;
                    _eventHandler.TargetElementId = selected.ElementId;
                    _externalEvent.Raise();
                }
            }
        }

        private async void OnAiAnalyzeClicked(object sender, RoutedEventArgs e)
        {
            var violations = _allItems.Where(i => i.Status == ComplianceStatus.NonCompliant && !i.IsResolved).ToList();
            string selectedCountry = (CmbCountry.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Spain";
            string selectedProvince = (CmbProvince.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Catalonia";

            _aiService.InitializeProjectContext(_doc.Title, selectedCountry, selectedProvince, _rooms, violations);

            ChatMessages.Add(new ChatMessage { Text = "Initiating comprehensive architectural analysis based on local regulations...", IsUser = true });
            ScrollChatToBottom();

            try
            {
                string aiReport = await _aiService.SendMessageAsync("Please provide a professional executive summary of this project's compliance and suggest solutions for the identified violations.");
                ChatMessages.Add(new ChatMessage { Text = aiReport, IsUser = false });
            }
            catch (Exception ex)
            {
                ChatMessages.Add(new ChatMessage { Text = $"❌ AI Error: {ex.Message}", IsUser = false });
            }
            ScrollChatToBottom();
        }

        private async void OnSendChatClicked(object sender, RoutedEventArgs e)
        {
            await SendUserMessage();
        }

        private async void OnChatInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SendUserMessage();
            }
        }

        private async Task SendUserMessage()
        {
            string msg = TxtChatInput.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            TxtChatInput.Text = "";
            ChatMessages.Add(new ChatMessage { Text = msg, IsUser = true });
            ScrollChatToBottom();

            string response = await _aiService.SendMessageAsync(msg);
            ChatMessages.Add(new ChatMessage { Text = response, IsUser = false });
            ScrollChatToBottom();
        }

        private void ScrollChatToBottom()
        {
            ChatScrollViewer.ScrollToEnd();
        }

        private void OnExportMarkdownClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string path = Path.Combine(desktop, $"Compliance_Report_{DateTime.Now:yyyyMMdd_HHmmss}.md");

                var sb = new StringBuilder();
                sb.AppendLine("# Architectural Compliance Audit Report");
                sb.AppendLine($"**Project:** {_doc.Title}");
                sb.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm}");
                sb.AppendLine($"**Region:** {(CmbCountry.SelectedItem as ComboBoxItem)?.Content} - {(CmbProvince.SelectedItem as ComboBoxItem)?.Content}\n");

                sb.AppendLine("## Compliance Matrix");
                sb.AppendLine("| Status | Element | Parameter Checked | Measured | Required | Regulation | Remarks |");
                sb.AppendLine("|---|---|---|---|---|---|---|");

                foreach (var item in _allItems.Where(i => !i.IsResolved))
                {
                    sb.AppendLine($"| {item.StatusDisplay} | {item.ElementIdentifier} | {item.ParameterChecked} | {item.MeasuredValue} | {item.RequiredValue} | {item.RegulationReference} | {item.Message} |");
                }

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                System.Windows.MessageBox.Show($"Markdown report exported successfully to:\n{path}");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to export report: {ex.Message}");
            }
        }

        private void OnExportCsvClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string path = Path.Combine(desktop, $"Compliance_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                var sb = new StringBuilder();
                sb.AppendLine("Status,Type,Element,ParameterChecked,Measured,Required,Regulation,Remarks");

                foreach (var item in _allItems.Where(i => !i.IsResolved))
                {
                    sb.AppendLine($"\"{item.StatusDisplay}\",\"{item.ElementType}\",\"{item.ElementIdentifier}\",\"{item.ParameterChecked}\",\"{item.MeasuredValue}\",\"{item.RequiredValue}\",\"{item.RegulationReference}\",\"{item.Message}\"");
                }

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                System.Windows.MessageBox.Show($"CSV report exported successfully to:\n{path}");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to export CSV: {ex.Message}");
            }
        }
    }

    public class ChatMessage
    {
        public string Text { get; set; }
        public bool IsUser { get; set; }
        public string BackgroundColor => IsUser ? "#89B4FA" : "#313244";
        public string ForegroundColor => IsUser ? "#11111B" : "#CDD6F4";
        public HorizontalAlignment Alignment => IsUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    }
}



