using System.Linq;
using Xceed.Document.NET;

namespace Template_TU.Services.DocumentProcessing;

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
        return;

        string Replace(string str) => _tempVariables.TryGetValue(str, out var value) ? value : "${" + str + "}";
    }

    private void ProcessVariables()
    {
        var replaceTextOptions = new FunctionReplaceTextOptions
        {
            FindPattern = @"\$\{(.*?)\}",
            RegexMatchHandler = Replace,
        };
        _document.ReplaceText(replaceTextOptions);
        return;

        string Replace(string str) => _data.Variables.FirstOrDefault(v => v.Name == str)?.Value ?? "${" + str + "}";
    }
}