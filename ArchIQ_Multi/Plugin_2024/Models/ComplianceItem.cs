using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RevitCataloniaChecker.Models
{
    public enum ComplianceStatus
    {
        Compliant,
        Warning,
        NonCompliant
    }

    public class ComplianceItem : INotifyPropertyChanged
    {
        public long ElementId { get; set; }
        public string ElementType { get; set; } = "Room"; 
        public string ElementIdentifier { get; set; } = string.Empty; 
        public string ParameterChecked { get; set; } = string.Empty;
        public string MeasuredValue { get; set; } = string.Empty;
        public string RequiredValue { get; set; } = string.Empty;
        public string RegulationReference { get; set; } = string.Empty;
        
        private ComplianceStatus _status;
        public ComplianceStatus Status 
        { 
            get => _status; 
            set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusDisplay)); OnPropertyChanged(nameof(StatusColor)); } 
        }

        private bool _isResolved;
        public bool IsResolved
        {
            get => _isResolved;
            set { _isResolved = value; OnPropertyChanged(); }
        }

        public string Message { get; set; } = string.Empty;
        public string AiSuggestion { get; set; } = string.Empty;

        public string StatusDisplay => Status switch
        {
            ComplianceStatus.Compliant => "✅ Pass",
            ComplianceStatus.Warning => "⚠️ Warn",
            ComplianceStatus.NonCompliant => "❌ Fail",
            _ => "Unknown"
        };

        public string StatusColor => Status switch
        {
            ComplianceStatus.Compliant => "#A6E3A1", // Green
            ComplianceStatus.Warning => "#F9E2AF",   // Yellow
            ComplianceStatus.NonCompliant => "#F38BA8", // Red
            _ => "#6C7086"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
