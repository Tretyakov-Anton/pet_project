using System.Linq;
using System.Text.RegularExpressions;
using Xceed.Document.NET;

namespace Template_TU.Services.DocumentProcessing;

public partial class DocumentProcessorCore
{
    [GeneratedRegex(@"^\s*\${(?<name>.*)}\s*$")]
    private static partial Regex VariableParagraphRegex();

    private static double CmToSize(double cm) => 28.35142857142857f * cm; //496.15 / 17.5

    private const double LineSpacing15 = 18; // 15 = 1.25

    // 18 / 1.5 = 15 / 1.25   <~>   18 / 1.5 = x / height   >>   x = 18 * height / 1.5
    private static double GetLineSpacing(double height) => 18 * height / 1.5;

    private void ProcessSpecialBlockVariables()
    {
        var paragraphs = _document.Paragraphs.ToList();
        for (int i = 0; i < paragraphs.Count; i++)
        {
            var paragraph = paragraphs[i];
            var match = VariableParagraphRegex().Match(paragraph.Text);
            if (match.Success)
            {
                var variableName = match.Groups["name"].Value;

                if (ProcessSpecialBlockVariable(paragraph, variableName)) continue;
            }

            // TODO: inline variables?
        }
    }

    private bool ProcessSpecialBlockVariable(Paragraph paragraph, string variableName)
    {
        switch (variableName)
        {
            case "таблицы 1":
                ProcessElectricalParameterTableVariable(paragraph);
                break;
            case "таблицы 2":
                ProcessOperatingConditionsTableVariable(paragraph);
                break;
            case "описания":
                ProcessDescriptionVariable(paragraph);
                break;
            default:
                return false;
        }

        return true;
    }

    private void ProcessDescriptionVariable(Paragraph paragraph)
    {
        var formatting = paragraph.MagicText[0].formatting;
        foreach (var element in _data.Elements)
        {
            var e = element.Value;
            var p = paragraph.InsertParagraphBeforeSelf(
                $"Микросхема {e.Name} представляет собой {e.Description}.",
                false, formatting);

            p.SpacingLine(paragraph.LineSpacing);
            p.IndentationFirstLine = paragraph.IndentationFirstLine;
        }

        paragraph.Remove(false);
    }
}