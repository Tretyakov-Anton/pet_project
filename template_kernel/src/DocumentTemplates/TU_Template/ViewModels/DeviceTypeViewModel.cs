using System;
using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;
using System.Reactive;
using TU_Template.Models;

namespace TU_Template.ViewModels;

public class DeviceGroupViewModel : ViewModelBase
{
    private readonly DeviceGroup _model;
    public DeviceGroup Model => _model;
    private readonly ApplicationGViewModel _parentViewModel;

    public ObservableCollection<DeviceTypeViewModel> Types { get; }
    
    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            this.RaiseAndSetIfChanged(ref _name, value);
            // Синхронизируем с моделью
            if (_model != null)
            {
                _model.Name = value;
            }
        }
    }

    private string _typeName = "";
    public string TypeName
    {
        get => _typeName;
        set
        {
            this.RaiseAndSetIfChanged(ref _typeName, value);
            // Синхронизируем с коллекцией Types в модели
            if (_model != null)
            {
                // Очищаем старые типы
                _model.Types.Clear();
                
                // Парсим строку типов (разделенных запятой) и добавляем в модель
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var types = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t));
                    
                    foreach (var typeName in types)
                    {
                        _model.Types.Add(new DeviceType(typeName));
                    }
                }
            }
        }
    }

    public ReactiveCommand<Unit, Unit> AddTypeCommand { get; }
    public ReactiveCommand<DeviceTypeViewModel, Unit> RemoveTypeCommand { get; }
    public bool IsNameReadOnly { get; }

    public DeviceGroupViewModel(DeviceGroup model, ApplicationGViewModel parentViewModel)
    {
        _model = model;
        _parentViewModel = parentViewModel;
        _name = model.Name;
        IsNameReadOnly = model.IsPredefined;
        Types = new ObservableCollection<DeviceTypeViewModel>();

        foreach (var type in model.Types)
        {
            var vm = new DeviceTypeViewModel(type);
            UpdateIsCustom(vm);
            Types.Add(vm);
        }
        
        // Инициализируем TypeName из коллекции Types модели
        if (model.Types.Count > 0)
        {
            _typeName = string.Join(", ", model.Types.Select(t => t.Name));
        }

        AddTypeCommand = ReactiveCommand.Create(() =>
        {
            // Ограничение: максимум 5 приборов
            if (Types.Count >= 5)
                return;

            var newModel = new DeviceType("Новый тип");
            _model.Types.Add(newModel);
            var newVm = new DeviceTypeViewModel(newModel);
            UpdateIsCustom(newVm);
            newVm.Name = string.Empty;
            newVm.UpdateSuggestions(_parentViewModel.AvailableDevices);
            // Показать подсказки сразу после добавления
            newVm.IsSuggestionsVisible = true;
            Types.Add(newVm);
        });

        RemoveTypeCommand = ReactiveCommand.Create<DeviceTypeViewModel>(type =>
        {
            if (type == null || !Types.Contains(type)) return;

            var modelType = _model.Types.FirstOrDefault(t => t == type.Model || t.Name == type.Name);
            if (modelType != null)
            {
                _model.Types.Remove(modelType);
            }
            Types.Remove(type);
        });
    }

    private void UpdateIsCustom(DeviceTypeViewModel vm)
    {
        vm.IsCustom = !_parentViewModel.AvailableDevices.Contains(vm.Name);
        vm.IsReadOnly = _parentViewModel.AvailableDevices.Contains(vm.Name);
        vm.UpdateSuggestions(_parentViewModel.AvailableDevices);
    }
}