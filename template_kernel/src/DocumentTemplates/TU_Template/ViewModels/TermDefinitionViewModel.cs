using ReactiveUI;
using TU_Template.Models;

namespace TU_Template.ViewModels;

public class TermDefinitionViewModel : ViewModelBase
{
    private readonly TermDefinition _model;
    private readonly ApplicationAViewModel _parentViewModel;

    /// <summary>Модель, которую обёртывает этот элемент. Нужна для удаления по ссылке.</summary>
    public TermDefinition Model => _model;

    private string _term;
    public string Term
    {
        get => _term;
        set
        {
            this.RaiseAndSetIfChanged(ref _term, value);
            _model.Term = value;
        }
    }

    private string _symbol;
    public string Symbol
    {
        get => _symbol;
        set
        {
            this.RaiseAndSetIfChanged(ref _symbol, value);
            _model.Symbol = value;
        }
    }

    private string _definition;
    public string Definition
    {
        get => _definition;
        set
        {
            this.RaiseAndSetIfChanged(ref _definition, value);
            _model.Definition = value;
        }
    }

    private string _category;
    public string Category
    {
        get => _category;
        set
        {
            this.RaiseAndSetIfChanged(ref _category, value);
            _model.Category = value;
        }
    }

    private bool _isCustom;
    public bool IsCustom
    {
        get => _isCustom;
        set => this.RaiseAndSetIfChanged(ref _isCustom, value);
    }

    public TermDefinitionViewModel(TermDefinition model, ApplicationAViewModel parentViewModel)
    {
        _model = model;
        _parentViewModel = parentViewModel;
        
        _term = model.Term;
        _symbol = model.Symbol;
        _definition = model.Definition;
        _category = model.Category;
        _isCustom = model.IsCustom;
    }
}