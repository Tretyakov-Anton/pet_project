using System.Linq;
using Xceed.Document.NET;

namespace TU_Template.Services.DocumentProcessing;

public partial class DocumentProcessorCore
{
    private void ProcessTempVariables()
    {
        var replaceTextOptions = new FunctionReplaceTextOptions
        {
            FindPattern = @"\$\{(.*?)\}",
            RegexMatchHandler = Replace,
        };
        _document.ReplaceText(replaceTextOptions);

        string Replace(string str)
        {
            var key = (str ?? "").Trim();

            if (_tempVariables.TryGetValue(key, out var value))
                return value;

            var altKey = _tempVariables.Keys.FirstOrDefault(k => string.Equals(k, key, System.StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(altKey))
                return _tempVariables[altKey];

            return "${" + key + "}";
        }
    }

    private void ProcessVariables()
    {
        var replaceTextOptions = new FunctionReplaceTextOptions
        {
            FindPattern = @"\$\{(.*?)\}",
            RegexMatchHandler = Replace,
        };
        _document.ReplaceText(replaceTextOptions);

        string Replace(string str)
        {
            var key = (str ?? "").Trim();
            var match = _data.Variables.FirstOrDefault(v => string.Equals(v.Name, key, System.StringComparison.OrdinalIgnoreCase));
            return match?.Value ?? "${" + key + "}";
        }
    }
}


