using Template_TU.Models;
using TemplateCore;
using TemplatesCore;

namespace Template_TU
{
    public class Template_TUModule : ITemplateModule
    {
        public string Name => "Template_TU";
        public ITemplateData TemplateData { get; }

        public Template_TUModule()
        {
            TemplateData = new TemplateData();
        }
    }
}
