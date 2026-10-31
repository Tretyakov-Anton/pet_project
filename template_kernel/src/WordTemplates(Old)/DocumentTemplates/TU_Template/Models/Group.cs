using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

    public Group(string name = "")
    {
        _name = name;

        ElectricalParameters = new();
        OperatingConditionsParameters = new();
    }

    public ObservableCollection<Element> Elements { get; } = new();
    [ObservableProperty]
    private List<string> _deviceTypes = new();
    partial void OnElectricalParametersChanged(ObservableCollection<ElectricalParameter>? oldValue, ObservableCollection<ElectricalParameter> newValue)
    {
        if (oldValue != null) oldValue.CollectionChanged -= OnCollectionChanged;
        newValue.CollectionChanged += OnCollectionChanged;

        void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => SyncParameters();

        void SyncParameters()
        {
            Debug.WriteLine("Sync parameters");
            foreach (var element in Elements)
            {
                // Add new parameters
                foreach (var parameter in ElectricalParameters)
                {
                    if (element.ParameterValues.All(v => v.Parameter != parameter))
                    {
                        element.ParameterValues.Add(new(parameter));
                    }
                }

                // Remove unnecessary parameters
                var unnecessary = element.ParameterValues.Where(v => !ElectricalParameters.Contains(v.Parameter)).ToList();
                foreach (var parameterValue in unnecessary)
                {
                    element.ParameterValues.Remove(parameterValue);
                }
            }
        }

        SyncParameters();
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