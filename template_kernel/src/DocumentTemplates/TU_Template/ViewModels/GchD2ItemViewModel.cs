using ReactiveUI;

namespace TU_Template.ViewModels;

public class GchD2ItemViewModel : ViewModelBase
{
    public Models.GchD2Item? Model { get; }

    private string _caseName = "";
    public string CaseName
    {
        get => _caseName;
        set
        {
            this.RaiseAndSetIfChanged(ref _caseName, value);
            if (Model != null) Model.CaseName = value;
        }
    }

    private string _unifiedGch = "";
    public string UnifiedGch
    {
        get => _unifiedGch;
        set
        {
            this.RaiseAndSetIfChanged(ref _unifiedGch, value);
            if (Model != null) Model.UnifiedGch = value;
        }
    }

    private string _descriptionCode = "";
    public string DescriptionCode
    {
        get => _descriptionCode;
        set
        {
            this.RaiseAndSetIfChanged(ref _descriptionCode, value);
            if (Model != null) Model.DescriptionCode = value;
        }
    }

    private string _descriptionName = "";
    public string DescriptionName
    {
        get => _descriptionName;
        set
        {
            this.RaiseAndSetIfChanged(ref _descriptionName, value);
            if (Model != null) Model.DescriptionName = value;
        }
    }

    public GchD2ItemViewModel()
    {
    }

    public GchD2ItemViewModel(Models.GchD2Item model)
    {
        Model = model;
        _caseName = model.CaseName;
        _unifiedGch = model.UnifiedGch;
        _descriptionCode = model.DescriptionCode;
        _descriptionName = model.DescriptionName;
    }
}

