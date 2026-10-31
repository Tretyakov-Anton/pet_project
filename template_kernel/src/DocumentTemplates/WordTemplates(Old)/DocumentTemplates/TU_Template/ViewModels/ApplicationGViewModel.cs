using System.Collections.ObjectModel;
using System.Windows.Input;
using ReactiveUI;
using Avalonia.Controls;
using TU_Template.Models;

namespace TU_Template.ViewModels
{
    public class DeviceType
    {
        public string Name { get; set; }
    }
    
    public class DeviceGroup
    {
        public string Name { get; set; }
        public ObservableCollection<DeviceType> Types { get; set; } = new();
        public ICommand AddTypeCommand { get; }
        public ICommand RemoveTypeCommand { get; }
        public DeviceGroup(string name)
        {
            Name = name;
            AddTypeCommand = ReactiveCommand.Create(() => Types.Add(new DeviceType { Name = "Новый тип" }));
            RemoveTypeCommand = ReactiveCommand.Create<DeviceType>(type =>
            {
                if (type != null && Types.Contains(type))
                    Types.Remove(type);
            });
        }
    }
    
    public class ApplicationGViewModel : ViewModelBase
    {
        public ObservableCollection<DeviceGroup> DeviceGroups { get; } = new();

        public ICommand AddDeviceCommand { get; }
        private readonly Window _parentWindow;
        private readonly TemplateData _templateData;
        public ApplicationGViewModel() : this(null, null) { }
        public ApplicationGViewModel(Window parentWindow, TemplateData templateData)
        {
            _parentWindow = parentWindow;
            _templateData = templateData ?? new TemplateData();
            var names = new[]
            {
                "Источники питания",
                "Генератор СВЧ сигналов",
                "Амперметр",
                "Вольтметр",
                "Анализатор спектра"
            };

            foreach (var name in names)
            {
                DeviceGroups.Add(new DeviceGroup(name));
            }

            AddDeviceCommand = ReactiveCommand.Create(() =>
            {
                DeviceGroups.Add(new DeviceGroup("Новая группа"));
            });
        }
    }
}
