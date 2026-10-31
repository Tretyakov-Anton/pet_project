using CommunityToolkit.Mvvm.ComponentModel;

namespace TemplatesCore
{
    public class ViewModelBase : ObservableObject
    {
        public ITemplateData TemplateData { get; }
    }
}
