import re

with open(r'c:\Users\Soheil\Desktop\ArchIQ_SourceCode\RevitCataloniaChecker\Services\GeminiAiService.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# find SetApiKey
if 'public void SetApiKey' in content:
    content = re.sub(r'string json = .*?;', 'string json = "{\\"gemini_api_key\\":\\"" + key + "\\"}";', content)

with open(r'c:\Users\Soheil\Desktop\ArchIQ_SourceCode\RevitCataloniaChecker\Services\GeminiAiService.cs', 'w', encoding='utf-8') as f:
    f.write(content)
