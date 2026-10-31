using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xceed.Document.NET;

namespace TU_Template.Services.DocumentProcessing;

public partial class DocumentProcessorCore
{
    [GeneratedRegex(@"^\$\?{(?<name>.*)}")]
    private static partial Regex ConditionalRowRegex();

    [GeneratedRegex(@"^\s*\$\?{(?<name>.*)}\s*$")]
    private static partial Regex ConditionalParagraphRegex();

    [GeneratedRegex(@"^\s*\$\?\\\s*$")]
    private static partial Regex ConditionalEndParagraphRegex();

    private void ProcessConditions()
    {
        List<Paragraph> paragraphsToRemove = new();

        var e = _document.Paragraphs.GetEnumerator();
        while (e.MoveNext())
        {
            var match = ConditionalParagraphRegex().Match(e.Current!.Text);
            if (match.Success == false) continue;
            var variableName = match.Groups["name"].Value;
            HandleCondition(variableName);
        }

        void HandleCondition(string variableName)
        {
            paragraphsToRemove.Add(e.Current);
            bool.TryParse(_data.Variables.FirstOrDefault(v => v.Name == variableName)?.Value ?? "false", out bool value);

            while (e.MoveNext())
            {
                if (ConditionalEndParagraphRegex().IsMatch(e.Current!.Text))
                {
                    paragraphsToRemove.Add(e.Current);
                    return;
                }

                var match = ConditionalParagraphRegex().Match(e.Current!.Text);
                if (match.Success)
                {
                    var variable2Name = match.Groups["name"].Value;
                    HandleCondition(variable2Name);
                }

                if (!value)
                {
                    paragraphsToRemove.Add(e.Current);
                }
            }
        }

        e.Dispose();

        foreach (var paragraph in paragraphsToRemove)
        {
            paragraph.Remove(false);
        }
    }
}


