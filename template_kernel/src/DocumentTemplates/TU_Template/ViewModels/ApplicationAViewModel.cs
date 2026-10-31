using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using ReactiveUI;
using System.Reactive;
using TU_Template.Models;
using Avalonia.Controls;

namespace TU_Template.ViewModels;

public class ApplicationAViewModel : ViewModelBase
{
    public ObservableCollection<TermDefinitionViewModel> TermDefinitions { get; }
    public ObservableCollection<TermDefinitionViewModel> FilteredTermDefinitions { get; }
    public ObservableCollection<string> AvailableCategories { get; }

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchText, value);
            UpdateFilter();
        }
    }

    public ReactiveCommand<Unit, Unit> AddTermCommand { get; }
    public ReactiveCommand<TermDefinitionViewModel, Unit> RemoveTermCommand { get; }
    public ReactiveCommand<Unit, Unit> AddCategoryCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearSearchCommand { get; }

    private readonly Window? _parentWindow;
    private readonly TemplateData _templateData;

    /// <summary>Коллекция модели, на изменения которой мы сейчас подписаны.</summary>
    private ObservableCollection<TermDefinition>? _attachedModelCollection;

    public ApplicationAViewModel(Window? parentWindow, TemplateData templateData)
    {
        _parentWindow = parentWindow;
        _templateData = templateData;

        TermDefinitions = new ObservableCollection<TermDefinitionViewModel>();
        FilteredTermDefinitions = new ObservableCollection<TermDefinitionViewModel>();
        AvailableCategories = new ObservableCollection<string>();

        InitializeCategories();

        // Предустановленные термины добавляем ТОЛЬКО в пустой проект.
        // Раньше они добавлялись безусловно при каждом создании ViewModel и дублировались.
        if (_templateData.TermDefinitions.Count == 0)
            InitializePredefinedTerms();

        AttachModelCollection();

        // Загрузка проекта заменяет содержимое (а иногда и сам экземпляр) коллекции.
        // Без этой подписки вкладка продолжала показывать прежние, уже отсоединённые
        // от TemplateData объекты, и правки пользователя не доходили до документа.
        _templateData.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TemplateData.TermDefinitions))
                AttachModelCollection();
        };

        // Синхронизация фильтра при изменении коллекции представления
        TermDefinitions.CollectionChanged += OnTermDefinitionsChanged;

        AddTermCommand = ReactiveCommand.Create(() =>
        {
            // Добавляем только в модель — представление пересоберётся по событию коллекции.
            _templateData.TermDefinitions.Add(
                new TermDefinition("Новый термин", "", "Введите определение", "Общие"));
        });

        RemoveTermCommand = ReactiveCommand.Create<TermDefinitionViewModel>(term =>
        {
            // Удаляем по ссылке на модель, а не по совпадению названия:
            // при одинаковых или переименованных терминах поиск по имени удалял не то.
            if (term?.Model == null) return;
            _templateData.TermDefinitions.Remove(term.Model);
        });

        AddCategoryCommand = ReactiveCommand.Create(() =>
        {
            var newCategory = "Новая категория";
            if (!AvailableCategories.Contains(newCategory))
            {
                AvailableCategories.Add(newCategory);
            }
        });

        ClearSearchCommand = ReactiveCommand.Create(() =>
        {
            SearchText = "";
            UpdateFilter();
        });

        UpdateFilter();
    }

    private void AttachModelCollection()
    {
        if (!ReferenceEquals(_attachedModelCollection, _templateData.TermDefinitions))
        {
            if (_attachedModelCollection != null)
                _attachedModelCollection.CollectionChanged -= OnModelCollectionChanged;

            _attachedModelCollection = _templateData.TermDefinitions;
            _attachedModelCollection.CollectionChanged += OnModelCollectionChanged;
        }

        RebuildFromModel();
    }

    private void OnModelCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildFromModel();

    private void RebuildFromModel()
    {
        TermDefinitions.Clear();
        foreach (var term in _templateData.TermDefinitions)
            TermDefinitions.Add(new TermDefinitionViewModel(term, this));

        UpdateFilter();
    }

    private void OnTermDefinitionsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateFilter();

    private void UpdateFilter()
    {
        var query = (SearchText ?? "").Trim();
        var queryLower = query.ToLowerInvariant();

        FilteredTermDefinitions.Clear();

        foreach (var vm in TermDefinitions)
        {
            if (string.IsNullOrEmpty(query))
            {
                FilteredTermDefinitions.Add(vm);
                continue;
            }

            bool matches =
                (vm.Term ?? "").ToLowerInvariant().Contains(queryLower) ||
                (vm.Symbol ?? "").ToLowerInvariant().Contains(queryLower) ||
                (vm.Category ?? "").ToLowerInvariant().Contains(queryLower) ||
                (vm.Definition ?? "").ToLowerInvariant().Contains(queryLower);

            if (matches)
                FilteredTermDefinitions.Add(vm);
        }
    }

    private void InitializeCategories()
    {
        var defaultCategories = new[]
        {
            "Общие",
            "Электрические параметры",
            "Частотные характеристики",
            "Температурные характеристики",
            "Механические параметры"
        };

        foreach (var category in defaultCategories)
        {
            AvailableCategories.Add(category);
        }
    }

    private void InitializePredefinedTerms()
    {
        var predefinedTerms = new[]
        {
            new TermDefinition("Диапазон входных частот", "Δfвх",
                "Для умножителей частоты — диапазон, в котором коэффициент умножения соответствует заданному значению и значение потерь преобразования остается в пределах норм, установленных в 7.3.1.1.\nДля делителей частоты — диапазон, в котором коэффициент деления соответствует заданному значению и значение выходной мощности остается в пределах норм, установленных в 7.3.1.1",
                "Частотные характеристики"),
            new TermDefinition("Потери преобразования", "αпрб",
                "Отношение уровня мощности сигнала на входе к уровню мощности полезного сигнала на выходе, выраженное в дБ",
                "Электрические параметры"),
            new TermDefinition("Ток потребления", "Iпот",
                "Ток, потребляемый микросхемой от источника питания в рабочем режиме",
                "Электрические параметры")
        };

        foreach (var term in predefinedTerms)
        {
            term.IsCustom = false;
            _templateData.TermDefinitions.Add(term);
        }
    }
}
