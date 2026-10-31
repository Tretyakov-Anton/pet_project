using System.Collections.Generic;
using TU_Template.Models;
using Xceed.Words.NET;

namespace TU_Template.Services.DocumentProcessing;

public interface IDocumentProcessor
{
    DocX Process(DocX document, TemplateData data);
}

public class DocumentProcessor : IDocumentProcessor
{
    /// <summary>
    /// Значения переменных последней обработки. Используются, чтобы дописать
    /// колонтитулы в сохранённом файле (см. <see cref="HeaderFooterVariablePatcher"/>).
    /// </summary>
    public IReadOnlyDictionary<string, string> ResolvedVariables { get; private set; } =
        new Dictionary<string, string>();

    public DocX Process(DocX document, TemplateData data)
    {
        DocumentProcessorCore processor = new(document, data);
        processor.Process();
        ResolvedVariables = processor.ResolvedVariables;
        return document;
    }
}


