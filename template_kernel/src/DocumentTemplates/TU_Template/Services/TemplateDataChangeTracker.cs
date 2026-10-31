using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using TU_Template.Models;

namespace TU_Template.Services;

/// <summary>
/// Следит за любыми изменениями данных проекта и сообщает о них одним событием.
///
/// Нужен для двух вещей: пометки «есть несохранённые изменения» и автосохранения.
/// Модель вложенная (группы → параметры → значения), поэтому подписки переустанавливаются
/// при каждом изменении состава коллекций. Повторная подписка безопасна: перед каждым
/// «+=» выполняется «-=», так что обработчик не навешивается дважды.
/// </summary>
public sealed class TemplateDataChangeTracker : IDisposable
{
    private readonly TemplateData _data;
    private bool _disposed;

    /// <summary>Возникает при любом изменении данных проекта.</summary>
    public event EventHandler? Changed;

    public TemplateDataChangeTracker(TemplateData data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        Rescan();
    }

    /// <summary>Переустанавливает подписки по всему дереву данных.</summary>
    public void Rescan()
    {
        if (_disposed) return;

        Attach(_data);

        AttachCollection(_data.Variables);
        AttachCollection(_data.Groups);
        AttachCollection(_data.DeviceGroups);
        AttachCollection(_data.TermDefinitions);
        AttachCollection(_data.GchD2Items);
        AttachCollection(_data.ApplicationVSectionB1);
        AttachCollection(_data.ApplicationVSectionB2);
        AttachCollection(_data.Elements as INotifyCollectionChanged, _data.Elements as IEnumerable);

        foreach (var group in _data.Groups)
        {
            AttachCollection(group.ElectricalParameters);
            AttachCollection(group.OperatingConditionsParameters);
            AttachCollection(group.ElectricalParametersNotes);
            AttachCollection(group.OperatingConditionsParametersNotes);

            foreach (var parameter in group.ElectricalParameters)
                AttachCollection(parameter.MeasurementModeValues);

            foreach (var parameter in group.OperatingConditionsParameters)
            {
                Attach(parameter.MaximumPermissible);
                Attach(parameter.Limit);
            }
        }

        foreach (var element in (System.Collections.Generic.IList<Element>)_data.Elements)
        {
            Attach(element);
            AttachCollection(element.ParameterValues);

            foreach (var value in element.ParameterValues)
                Attach(value.Limits);
        }

        foreach (var deviceGroup in _data.DeviceGroups)
            AttachCollection(deviceGroup.Types);
    }

    private void AttachCollection(object? collection)
        => AttachCollection(collection as INotifyCollectionChanged, collection as IEnumerable);

    private void AttachCollection(INotifyCollectionChanged? notifier, IEnumerable? items)
    {
        if (notifier != null)
        {
            notifier.CollectionChanged -= OnCollectionChanged;
            notifier.CollectionChanged += OnCollectionChanged;
        }

        if (items == null) return;

        foreach (var item in items)
            Attach(item);
    }

    private void Attach(object? item)
    {
        if (item is not INotifyPropertyChanged observable) return;

        observable.PropertyChanged -= OnPropertyChanged;
        observable.PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) => Notify();

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Состав коллекции изменился — в дереве могли появиться новые объекты.
        Rescan();
        Notify();
    }

    private void Notify()
    {
        if (_disposed) return;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() => _disposed = true;
}
