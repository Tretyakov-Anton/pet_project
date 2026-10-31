using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using TU_Template.Models;

namespace TU_Template.ViewModels;

public class DeviceTypeViewModel : ViewModelBase
{
    public DeviceType? Model { get; }
    private string _name = "";
    public string Name
    {
        get => _name;
        set
        {
            this.RaiseAndSetIfChanged(ref _name, value);
            if (Model != null)
            {
                Model.Name = value;
            }
        }
    }

    private bool _isDeleted;
    public bool IsDeleted
    {
        get => _isDeleted;
        set => this.RaiseAndSetIfChanged(ref _isDeleted, value);
    }

    private bool _isCustom = true;
    public bool IsCustom
    {
        get => _isCustom;
        set => this.RaiseAndSetIfChanged(ref _isCustom, value);
    }

    private bool _isReadOnly;
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => this.RaiseAndSetIfChanged(ref _isReadOnly, value);
    }

    private bool _isSuggestionsVisible;
    public bool IsSuggestionsVisible
    {
        get => _isSuggestionsVisible;
        set => this.RaiseAndSetIfChanged(ref _isSuggestionsVisible, value);
    }

    private ObservableCollection<string> _suggestions = new();
    public ObservableCollection<string> Suggestions
    {
        get => _suggestions;
        set => this.RaiseAndSetIfChanged(ref _suggestions, value);
    }

    public DeviceTypeViewModel(string name = "")
    {
        _name = name;
    }

    public DeviceTypeViewModel(DeviceType model)
    {
        Model = model;
        _name = model?.Name ?? "";
    }

    public void UpdateSuggestions(IEnumerable<string> availableDevices)
    {
        var safeDevices = availableDevices ?? System.Linq.Enumerable.Empty<string>();
        var query = Name?.Trim() ?? "";

        var queryLower = query.ToLower();
        var filtered = string.IsNullOrEmpty(query)
            // Если строка пуста — показываем первые элементы списка
            ? safeDevices.Where(d => !string.IsNullOrEmpty(d)).Take(10).ToList()
            // Иначе фильтруем по подстроке
            : safeDevices
                .Where(d => !string.IsNullOrEmpty(d) && d.ToLower().Contains(queryLower))
                .Take(10)
                .ToList();

        Suggestions.Clear();
        foreach (var item in filtered)
        {
            Suggestions.Add(item);
        }

        var isExact = safeDevices.Contains(Name);
        // Если точное совпадение уже выбрано — скрываем список, чтобы не было дубликата справа
        IsSuggestionsVisible = isExact ? false : (Suggestions.Count > 0);
    
        // Если текущее значение есть в списке — делаем его read-only
        IsReadOnly = isExact;
        IsCustom = !isExact;
    }
}