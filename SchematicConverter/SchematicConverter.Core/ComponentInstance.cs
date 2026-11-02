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

    
    public ComponentInstance(string positionalDestignation, ComponentType type, string nominal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(positionalDestignation);
        ArgumentException.ThrowIfNullOrWhiteSpace(nominal);
        Enum.IsDefined(type);
        PositionalDestignation = positionalDestignation;
        Type = type;
        Nominal = nominal;
    }



}
