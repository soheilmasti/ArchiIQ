using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RevitCataloniaChecker.Models;

namespace RevitCataloniaChecker.Services
{
    public class GeminiAiService
    {
        private readonly HttpClient _httpClient;
        private string? _apiKey;
        private string _systemInstruction = "";
        
        // Chat history schema for Gemini API
        private readonly List<object> _chatHistory = new List<object>();

        public GeminiAiService()
        {
            _httpClient = new HttpClient();
            LoadApiKey();
        }

        private void LoadApiKey()
        {
            try
            {
                string configPath = @"c:\Users\Soheil\Downloads\Jarvis\config\api_keys.json";
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("gemini_api_key", out var keyElem))
                    {
                        _apiKey = keyElem.GetString();
                    }
                }
            }
            catch
            {
                _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            }
        }

        public bool HasApiKey => !string.IsNullOrWhiteSpace(_apiKey);

        public void InitializeProjectContext(string projectName, string country, string province, List<RoomData> rooms, List<ComplianceItem> nonCompliantItems, List<BomData> bom)
        {
            _chatHistory.Clear();

            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"You are ArchIQ, an elite AI Architectural Code Compliance Consultant.");
            promptBuilder.AppendLine($"Currently analyzing a BIM model in Revit for a project named '{projectName}'.");
            promptBuilder.AppendLine($"The selected jurisdiction is: Country = {country}, Province/Region = {province}.");
            promptBuilder.AppendLine($"Always answer questions specifically with this region's National Building Codes in mind (e.g. Mabhas 3 & 4 for Iran, or CTE & Decret 141 for Spain).");
            
            promptBuilder.AppendLine("\n### RAG DATA - EXTRACTED PROJECT SPACES:");
            foreach (var r in rooms)
            {
                promptBuilder.AppendLine($"- Room '{r.Name}' (No. {r.Number}): Area={r.AreaM2:F2}mA², Height={r.HeightM:F2}m, Windows={r.WindowCount} ({r.WindowAreaM2:F2}mA²)");
            }

            promptBuilder.AppendLine("\n### RAG DATA - IDENTIFIED CODE VIOLATIONS:");
            if (nonCompliantItems.Count == 0) promptBuilder.AppendLine("No violations found. The project is fully compliant!");
            foreach (var item in nonCompliantItems)
            {
                promptBuilder.AppendLine($"- [{item.ElementType}] {item.ElementIdentifier}: {item.ParameterChecked} measured {item.MeasuredValue} (Req: {item.RequiredValue}) - Rule: {item.RegulationReference}");
            }

                        promptBuilder.AppendLine("\n### RAG DATA - BILL OF MATERIALS (5D BIM):");
            if (bom != null && bom.Count > 0)
            {
                foreach (var b in bom)
                {
                    promptBuilder.AppendLine($"- {b.Category}: {b.ElementName} | Count: {b.Count} | Area: {b.TotalArea:F2} sqm | Vol: {b.TotalVolume:F2} cum");
                }
            }
            
            promptBuilder.AppendLine("\n### BEHAVIORAL INSTRUCTIONS FOR COST ESTIMATION:");
            promptBuilder.AppendLine("You are also a 5D BIM Cost Estimator. If the user asks for pricing or provides a CSV price list, use the BOM data to calculate total costs.");
            promptBuilder.AppendLine("Intelligently deduce the correct currency based on the Region (e.g. Euro for Spain, IRR/Tomans for Iran).");
            promptBuilder.AppendLine("If no price list is provided, use your knowledge base to estimate current global/regional market prices for the materials listed in the BOM.");
            
                        promptBuilder.AppendLine("\n### BEHAVIORAL INSTRUCTIONS:");
            promptBuilder.AppendLine("Act as a professional consultant. Discuss the violations, suggest architectural solutions (e.g., merging rooms, adding windows, lowering requirements based on exceptions), and answer any follow-up questions from the architect in an engaging chat format.");

            _systemInstruction = promptBuilder.ToString();

            // Save the AI Project Memory locally
            try
            {
                string memoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"ArchIQ_Memory_{projectName}.md");
                File.WriteAllText(memoryPath, _systemInstruction, Encoding.UTF8);
            }
            catch { }
        }

        public async Task<string> SendMessageAsync(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(_apiKey)) return "❌ No Gemini API Key configured. Please check your settings.";

            // Add user message to history
            _chatHistory.Add(new { role = "user", parts = new[] { new { text = userMessage } } });

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var requestBody = new
            {
                system_instruction = new { parts = new[] { new { text = _systemInstruction } } },
                contents = _chatHistory,
                generationConfig = new { temperature = 0.4, maxOutputTokens = 2048 }
            };

            string jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // Rollback user message if error
                    _chatHistory.RemoveAt(_chatHistory.Count - 1);
                    return $"AI API Error: {response.StatusCode}\n{responseString}";
                }

                using var responseDoc = JsonDocument.Parse(responseString);
                var candidates = responseDoc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() > 0)
                {
                    string aiReply = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
                    
                    // Add AI response to history
                    _chatHistory.Add(new { role = "model", parts = new[] { new { text = aiReply } } });
                    
                    return aiReply;
                }

                return "AI produced an empty response.";
            }
            catch (Exception ex)
            {
                _chatHistory.RemoveAt(_chatHistory.Count - 1);
                return $"Error contacting ArchIQ AI: {ex.Message}";
            }
        }
    }
}



