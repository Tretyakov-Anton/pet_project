namespace SchematicConverter.Core;
public class ComponentInstance
{
     //Позиционное обозначение
    public string ReferenceDesignator {get;}
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
            if (existingPin.Number == pin.Number)
            {
                throw new InvalidOperationException($"Пин с номером {pin.Number} уже существует у компонента.");
            }
        }
        _listPin.Add(pin);
    } 
    
    public ComponentInstance(string referenceDesignator, ComponentType type, string nominal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ReferenceDesignator);
        ArgumentException.ThrowIfNullOrWhiteSpace(nominal);
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "Недопустимое значение типа компонента.");
        }
        ReferenceDesignator = referenceDesignator;
        Type = type;
        Nominal = nominal;
    }



}
