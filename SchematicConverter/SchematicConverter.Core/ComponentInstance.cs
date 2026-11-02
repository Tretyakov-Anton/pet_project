using System.ComponentModel;

namespace SchematicConverter.Core;

public class ComponentInstance
{
     //Позиционное обозначение
    public string PositionalDestignation {get;}
    //Тип компонента, создается обьект перечисления ComponentType
    public ComponentType Type {get;}
    //Номинал
    public string Nominal {get;}

    //Приватный Список пинов
    private List<Pin> _listPin;
    //Публичный Список пинов
    public IReadOnlyList<Pin> Pins {get => _listPin;}

    public void AddPin(Pin pin)
    {
        
    } 
    
    public ComponentInstance(string positionalDestination, ComponentType type, string nominal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(positionalDestination);
        ArgumentException.ThrowIfNullOrWhiteSpace(nominal);
        Enum.IsDefined(type);
        PositionalDestignation = positionalDestination;
        Type = type;
        Nominal = nominal;
    }



}
