using Avalonia.Controls;
using TemplatesCore;

namespace TemplateCore
{
    public interface ITemplateModule
    {
        public string Name { get; }
        public ITemplateData TemplateData { get; }
    }

}
