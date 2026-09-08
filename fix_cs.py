import re

with open(r'c:\Users\Soheil\Desktop\ArchIQ_SourceCode\RevitCataloniaChecker\Views\MainWindow.xaml.cs', 'r', encoding='utf-8') as f:
    content = f.read()

content = content.replace('ChatList.ItemsSource = ChatMessages;', 'TxtApiKey.Text = _aiService.GetApiKey();\n            ChatList.ItemsSource = ChatMessages;')

method_str = '''
        private void OnSaveApiKeyClicked(object sender, RoutedEventArgs e)
        {
            _aiService.SetApiKey(TxtApiKey.Text.Trim());
            System.Windows.MessageBox.Show("API Key saved successfully!");
        }

        private void OnUploadCsvClicked'''

content = content.replace('        private void OnUploadCsvClicked', method_str)

with open(r'c:\Users\Soheil\Desktop\ArchIQ_SourceCode\RevitCataloniaChecker\Views\MainWindow.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(content)
