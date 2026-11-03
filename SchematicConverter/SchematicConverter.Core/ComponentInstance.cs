namespace SchematicConverter.Core;
public class ComponentInstance
{
     //Позиционное обозначение
    public string PositionalDestination {get;}
    //Тип компонента, создается обьект перечисления ComponentType
    public ComponentType Type {get;}
    //Номинал
    public string Nominal {get;}

    //Приватный Список пинов
    private List<Pin> _listPin = new();
    //Публичный Список пинов
    public IReadOnlyList<Pin> Pins {get => _listPin;}

    public void AddPin(Pin pin)
    {
        ArgumentNullException.ThrowIfNull(pin);
        foreach (var existingPin in _listPin)
        {
            if (existingPin.NumberPin == pin.NumberPin)
            {
                throw new InvalidOperationException($"Пин с номером {pin.NumberPin} уже существует у компонента.");
            }
        }
        _listPin.Add(pin);
    } 
    
    public ComponentInstance(string positionalDestination, ComponentType type, string nominal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(positionalDestination);
        ArgumentException.ThrowIfNullOrWhiteSpace(nominal);
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "Недопустимое значение типа компонента.");
        }
        PositionalDestination = positionalDestination;
        Type = type;
        Nominal = nominal;
    }



}
