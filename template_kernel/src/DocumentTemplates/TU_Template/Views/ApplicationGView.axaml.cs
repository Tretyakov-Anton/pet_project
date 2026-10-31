using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using TU_Template.ViewModels;

namespace TU_Template.Views;

public partial class ApplicationGView : UserControl
{
    public ApplicationGView()
    {
        InitializeComponent();
    }

    private async void OnTypeTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is DeviceTypeViewModel vm)
        {
            await Task.Delay(300); // debounce
            var availableDevices = (DataContext as ApplicationGViewModel)?.AvailableDevices ?? System.Linq.Enumerable.Empty<string>();
            vm.UpdateSuggestions(availableDevices);
            // Если поле пустое — раскрыть список подсказок
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                vm.IsSuggestionsVisible = vm.Suggestions.Count > 0;
                // не теряем фокус с текстбокса, чтобы список не закрывался
                textBox.CaretIndex = textBox.Text?.Length ?? 0;
            }
        }
    }

    private void OnSuggestionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.DataContext is DeviceTypeViewModel vm)
        {
            if (listBox.SelectedItem is string selectedValue)
            {
                // Устанавливаем выбранное значение только через VM, чтобы сохранить в TemplateData
                vm.Name = selectedValue;
                vm.IsReadOnly = true;
                vm.IsCustom = false;
                // Список спрячем, элементы не очищаем прямо в событии, чтобы не ломать SelectionModel
            }

            // Закрываем popup
            vm.IsSuggestionsVisible = false;

            // Убираем фокус с TextBox, чтобы не добавлялись лишние символы
            var focusableElement = this.FindControl<Button>("AddTypeButton"); // или любой другой элемент
            focusableElement?.Focus();

            // Сбросить выбор после завершения события, чтобы не было повторного триггера
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                listBox.SelectedIndex = -1;
            });
        }
    }
}