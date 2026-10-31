using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
namespace TU_Template.Views;

public partial class AddDeviceDialog : Window
{
    public string? SelectedDeviceName { get; private set; }
    public string? SelectedDeviceType { get; private set; }

    // Ленивая загрузка: по выбранному имени прибора получаем типы/модели
    private Func<string, Task<ObservableCollection<string>>>? _loadTypesForDeviceNameAsync;

    // Для редактирования: пока грузятся типы для выбранного имени, запоминаем нужный тип
    private string? _pendingSelectedDeviceType;

    // Защита от гонок: если пользователь быстро меняет имя, результаты старых запросов игнорируем
    private int _typesLoadRequestId;

    public AddDeviceDialog()
    {
        InitializeComponent();  
    }
        
    public void SetDeviceNames(IEnumerable<string> names)
    {
        DeviceNameComboBox.ItemsSource = names;
    }

    // Устанавливаем обработчик, который по имени прибора подгружает список типов/моделей
    public void SetDeviceTypesLoaderAsync(Func<string, Task<ObservableCollection<string>>> loader)
    {
        _loadTypesForDeviceNameAsync = loader;
    }

    // Предзаполнение данных для редактирования
    public void SetSelectedValues(string? deviceName, string? deviceType)
    {
        _pendingSelectedDeviceType = string.IsNullOrWhiteSpace(deviceType) ? null : deviceType.Trim();

        if (!string.IsNullOrWhiteSpace(deviceName))
        {
            DeviceNameComboBox.SelectedItem = deviceName;
            _ = UpdateTypesForSelectedNameAsync(); // подхватит _pendingSelectedDeviceType
        }
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        SelectedDeviceName = DeviceNameComboBox.SelectedItem?.ToString();
        SelectedDeviceType = DeviceTypeComboBox.SelectedItem?.ToString();
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void DeviceNameComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _ = UpdateTypesForSelectedNameAsync();
    }

    private async Task UpdateTypesForSelectedNameAsync()
    {
        var requestId = ++_typesLoadRequestId;
        var name = DeviceNameComboBox.SelectedItem?.ToString();

        if (string.IsNullOrWhiteSpace(name) || _loadTypesForDeviceNameAsync == null)
        {
            DeviceTypeComboBox.ItemsSource = null;
            DeviceTypeComboBox.SelectedIndex = -1;
            _pendingSelectedDeviceType = null;
            return;
        }

        ObservableCollection<string> types;
        try
        {
            types = await _loadTypesForDeviceNameAsync(name);
        }
        catch
        {
            types = new ObservableCollection<string>();
        }

        // Игнорируем устаревший результат
        if (requestId != _typesLoadRequestId)
            return;

        DeviceTypeComboBox.ItemsSource = types;

        if (types.Count == 0)
        {
            DeviceTypeComboBox.SelectedIndex = -1;
            _pendingSelectedDeviceType = null;
            return;
        }

        // Если редактируем и есть ожидаемый тип — выбираем его
        if (!string.IsNullOrWhiteSpace(_pendingSelectedDeviceType))
        {
            var pending = _pendingSelectedDeviceType!;
            // точное совпадение или case-insensitive
            var match = types.FirstOrDefault(t => string.Equals(t, pending, StringComparison.OrdinalIgnoreCase))
                         ?? (types.Contains(pending) ? pending : null);

            DeviceTypeComboBox.SelectedItem = match ?? types[0];
            _pendingSelectedDeviceType = null;
            return;
        }

        DeviceTypeComboBox.SelectedIndex = 0;
    }
}

