using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TU_Template.Views;

public partial class AddGchDialog : Window
{
    public string? SelectedCaseName { get; private set; }
    public string? SelectedUnifiedGch { get; private set; }
    public string? SelectedDescriptionCode { get; private set; }
    public string? SelectedDescriptionName { get; private set; }

    private readonly Dictionary<string, GchEntry> _entriesByCase = new();

    public AddGchDialog()
    {
        InitializeComponent();
    }

    public void SetEntries(IEnumerable<GchEntry> entries)
    {
        _entriesByCase.Clear();
        foreach (var e in entries)
        {
            if (string.IsNullOrWhiteSpace(e.CaseName))
                continue;

            _entriesByCase[e.CaseName] = e;
        }

        CaseComboBox.ItemsSource = _entriesByCase.Keys.OrderBy(x => x).ToList();
    }

    private void OnCaseSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (CaseComboBox.SelectedItem is string caseName &&
            _entriesByCase.TryGetValue(caseName, out var entry))
        {
            var gchList = new ObservableCollection<string>();
            if (!string.IsNullOrWhiteSpace(entry.UnifiedGch))
            {
                gchList.Add(entry.UnifiedGch);
            }

            GchComboBox.ItemsSource = gchList;
            GchComboBox.SelectedIndex = gchList.Count > 0 ? 0 : -1;

            var descCodes = new ObservableCollection<string>();
            if (!string.IsNullOrWhiteSpace(entry.DescriptionCode))
            {
                descCodes.Add(entry.DescriptionCode);
            }

            DescCodeComboBox.ItemsSource = descCodes;
            DescCodeComboBox.SelectedIndex = descCodes.Count > 0 ? 0 : -1;

            var descNames = new ObservableCollection<string>();
            if (!string.IsNullOrWhiteSpace(entry.DescriptionName))
            {
                descNames.Add(entry.DescriptionName);
            }

            DescNameComboBox.ItemsSource = descNames;
            DescNameComboBox.SelectedIndex = descNames.Count > 0 ? 0 : -1;
        }
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        SelectedCaseName = CaseComboBox.SelectedItem?.ToString();
        SelectedUnifiedGch = GchComboBox.SelectedItem?.ToString();
        SelectedDescriptionCode = DescCodeComboBox.SelectedItem?.ToString();
        SelectedDescriptionName = DescNameComboBox.SelectedItem?.ToString();
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    public sealed class GchEntry
    {
        public string CaseName { get; set; } = "";
        public string UnifiedGch { get; set; } = "";
        public string DescriptionCode { get; set; } = "";
        public string DescriptionName { get; set; } = "";
    }
}

