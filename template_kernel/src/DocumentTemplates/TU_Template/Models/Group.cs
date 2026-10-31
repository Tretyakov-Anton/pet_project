using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TU_Template.Models;

public partial class Group : ObservableObject
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private ObservableCollection<ElectricalParameter> _electricalParameters;
    [ObservableProperty] private ObservableCollection<Note> _electricalParametersNotes = new();
    [ObservableProperty] private ObservableCollection<OperatingConditionsParameter> _operatingConditionsParameters;
    [ObservableProperty] private ObservableCollection<Note> _operatingConditionsParametersNotes = new();

    /// <summary>Заголовки столбцов «Режим измерения» для таблицы 3: параметры операционных условий + fвх, Rн.</summary>
    public ObservableCollection<string> MeasurementModeColumnHeaders { get; } = new();

    public Group(string name = "")
    {
        _name = name;

        ElectricalParameters = new();
        OperatingConditionsParameters = new();
    }

    partial void OnElectricalParametersChanged(ObservableCollection<ElectricalParameter>? oldValue, ObservableCollection<ElectricalParameter> newValue)
    {
        if (oldValue != null) oldValue.CollectionChanged -= OnElectricalParametersCollectionChanged;
        newValue.CollectionChanged += OnElectricalParametersCollectionChanged;
        OnElectricalParametersCollectionChanged(null!, null!);

        if (oldValue != null) oldValue.CollectionChanged -= OnCollectionChanged;
        newValue.CollectionChanged += OnCollectionChanged;

        void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => SyncParameters();

        void SyncParameters()
        {
            Debug.WriteLine("Sync parameters");
            foreach (var element in Elements)
            {
                foreach (var parameter in ElectricalParameters)
                {
                    if (element.ParameterValues.All(v => v.Parameter != parameter))
                        element.ParameterValues.Add(new(parameter));
                }
                var unnecessary = element.ParameterValues.Where(v => !ElectricalParameters.Contains(v.Parameter)).ToList();
                foreach (var pv in unnecessary)
                    element.ParameterValues.Remove(pv);
            }
        }

        SyncParameters();
    }

    private void OnElectricalParametersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => EnsureLastParameterMeasurementModeValues();

    partial void OnOperatingConditionsParametersChanged(ObservableCollection<OperatingConditionsParameter>? oldValue, ObservableCollection<OperatingConditionsParameter> newValue)
    {
        if (oldValue != null)
        {
            oldValue.CollectionChanged -= OnOperatingConditionsParametersCollectionChanged;
            foreach (var p in oldValue)
                UnsubscribeFromParameterHeaderChanges(p);
        }
        if (newValue != null)
        {
            newValue.CollectionChanged += OnOperatingConditionsParametersCollectionChanged;
            foreach (var p in newValue)
                SubscribeToParameterHeaderChanges(p);
        }
        UpdateMeasurementModeColumnHeaders();
        SyncMeasurementModeValues();
    }

    private void OnOperatingConditionsParametersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (OperatingConditionsParameter p in e.OldItems)
                UnsubscribeFromParameterHeaderChanges(p);
        }
        if (e.NewItems != null)
        {
            foreach (OperatingConditionsParameter p in e.NewItems)
                SubscribeToParameterHeaderChanges(p);
        }
        UpdateMeasurementModeColumnHeaders();
        SyncMeasurementModeValues();
    }

    private void SubscribeToParameterHeaderChanges(OperatingConditionsParameter p) =>
        p.PropertyChanged += OnOperatingConditionsParameterPropertyChanged;

    private void UnsubscribeFromParameterHeaderChanges(OperatingConditionsParameter p) =>
        p.PropertyChanged -= OnOperatingConditionsParameterPropertyChanged;

    private void OnOperatingConditionsParameterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(OperatingConditionsParameter.Name) or nameof(OperatingConditionsParameter.Symbol))
            UpdateMeasurementModeColumnHeaders();
    }

    public ObservableCollection<Element> Elements { get; } = new();
    [ObservableProperty]
    private List<string> _deviceTypes = new();

    private static string GetMeasurementModeHeader(OperatingConditionsParameter p)
    {
        if (string.IsNullOrWhiteSpace(p.Name) || !p.Name.Contains(','))
            return string.IsNullOrWhiteSpace(p.Symbol) ? p.Name : p.Symbol;
        var unit = p.Name.Substring(p.Name.IndexOf(',') + 1).Trim();
        return string.IsNullOrWhiteSpace(p.Symbol) ? unit : $"{p.Symbol}, {unit}";
    }

    private void UpdateMeasurementModeColumnHeaders()
    {
        MeasurementModeColumnHeaders.Clear();
        foreach (var p in OperatingConditionsParameters)
            MeasurementModeColumnHeaders.Add(GetMeasurementModeHeader(p));
        MeasurementModeColumnHeaders.Add("fвх, ГГц");
        MeasurementModeColumnHeaders.Add("Rн, Ом");
    }

    private void SyncMeasurementModeValues()
    {
        var needed = OperatingConditionsParameters.Count + 2;
        foreach (var param in ElectricalParameters)
        {
            while (param.MeasurementModeValues.Count < needed)
                param.MeasurementModeValues.Add(new MeasurementModeCell());
            while (param.MeasurementModeValues.Count > needed)
                param.MeasurementModeValues.RemoveAt(param.MeasurementModeValues.Count - 1);
        }
    }

    private void EnsureLastParameterMeasurementModeValues()
    {
        if (ElectricalParameters.Count == 0) return;
        var needed = OperatingConditionsParameters.Count + 2;
        var last = ElectricalParameters[^1];
        while (last.MeasurementModeValues.Count < needed)
            last.MeasurementModeValues.Add(new MeasurementModeCell());
    }

    [RelayCommand]
    private void AddElectricalParameter() => ElectricalParameters.Add(new());

    [RelayCommand]
    private void RemoveElectricalParameter(ElectricalParameter p) => ElectricalParameters.Remove(p);

    [RelayCommand]
    private void AddOperatingConditionsParameter() => OperatingConditionsParameters.Add(new());

    [RelayCommand]
    private void RemoveOperatingConditionsParameter(OperatingConditionsParameter p) => OperatingConditionsParameters.Remove(p);

    [RelayCommand]
    private void AddElectricalParametersNote() => ElectricalParametersNotes.Add(new(""));

    [RelayCommand]
    private void RemoveElectricalParametersNote(Note n) => ElectricalParametersNotes.Remove(n);

    [RelayCommand]
    private void AddOperatingConditionsParametersNote() => OperatingConditionsParametersNotes.Add(new(""));

    [RelayCommand]
    private void RemoveOperatingConditionsParametersNote(Note n) => OperatingConditionsParametersNotes.Remove(n);

    public GroupJsonModel ToJsonModel() => new()
    {
        Name = Name,
        ElectricalParameters = ElectricalParameters.ToArray(),
        ElectricalParametersNotes = ElectricalParametersNotes.Select(n => n.Text).ToArray(),
        OperatingConditionsParameters = OperatingConditionsParameters.ToArray(),
        OperatingConditionsParametersNotes = OperatingConditionsParametersNotes.Select(n => n.Text).ToArray(),
    };
}

public partial class Note(string text) : ObservableObject
{
    [ObservableProperty] private string _text = text;
}

public class GroupJsonModel
{
    public string Name { get; set; } = "";
    public ElectricalParameter[] ElectricalParameters { get; set; } = [ ];
    public string[] ElectricalParametersNotes { get; set; } = [ ];
    public OperatingConditionsParameter[] OperatingConditionsParameters { get; set; } = [ ];
    public string[] OperatingConditionsParametersNotes { get; set; } = [ ];

    public Group ToGroup() => new(Name)
    {
        ElectricalParameters = new(ElectricalParameters),
        ElectricalParametersNotes = new(ElectricalParametersNotes.Select(n => new Note(n))),
        OperatingConditionsParameters = new(OperatingConditionsParameters),
        OperatingConditionsParametersNotes = new(OperatingConditionsParametersNotes.Select(n => new Note(n)))
    };
}