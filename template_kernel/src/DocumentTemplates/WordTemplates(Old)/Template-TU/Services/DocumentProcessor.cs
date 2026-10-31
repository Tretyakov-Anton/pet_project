using Template_TU.Models;
using Template_TU.Services.DocumentProcessing;
using Xceed.Words.NET;

namespace Template_TU.Services;

public interface IDocumentProcessor
{
    DocX Process(DocX document, TemplateData data);
}

public class DocumentProcessor : IDocumentProcessor
{
    public DocX Process(DocX document, TemplateData data)
    {
        DocumentProcessorCore processor = new(document, data);
        processor.Process();// запуск процесса обработки шаблона
        return document;
    }
}