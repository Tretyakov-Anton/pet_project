
namespace SchematicConverter.Core;

public class LTSpiceParser : INetlustParser
{   
    public ComponentInstance ParseLine(string line)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(line);
        var words = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 4)
        {
            throw new FormatException($"Строка нетлиста имеет неверный формат (менее 4 параметров): {line}");
        }
        var referenceDesignator = words[0];
        var nominal = words[^1];
        var firstLetter = char.ToUpperInvariant(words[0][0]);
        ComponentType componentType;
        switch (firstLetter)
        {
            case 'R':
                componentType =  ComponentType.Resistor;
                break;
            case 'C':
                componentType = ComponentType.Capasistance;
                break;
            case 'L':
                componentType = ComponentType.Inductor;
                break;
            case 'V':
                componentType = ComponentType.VoltageSource;
                break;
            case 'Q':
                componentType = ComponentType.Transistor_BJT;
                break;
            case 'M':
                componentType = ComponentType.Transistor_MOSFET;
                break;
            case 'I':
                componentType = ComponentType.CurrentSource;
                break;
            default:
                throw new InvalidOperationException($"Такого компонента не существует");
        }
        ComponentInstance component = new ComponentInstance(referenceDesignator, componentType, nominal);

        for(int i = 1; i < words.Length-1; i++)
        {  
            Pin pin = new Pin(i, words[i]);
            component.AddPin(pin);
        }
        return component;
    }


    private NetType ClassifyNet(string nameNode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameNode);
        switch (nameNode.ToUpperInvariant())
        {
            case "0" or "GND":
                return NetType.Ground;
            case "VCC" or "VDD" or "VSS" or "VEE":
                return NetType.Power;
            default:
                return NetType.Signal;
        }
    }

    public Circuit Parse(string netlistText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(netlistText);
        Circuit circuit = new Circuit();
        var lines = netlistText.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string lineNetlist;
        ComponentInstance component;
        foreach(var item in lines)
        {
            lineNetlist = item.Trim();
            if(lineNetlist.StartsWith("*") || lineNetlist.StartsWith(".") || lineNetlist.Length == 0)
            {
                continue;
            }
            component = ParseLine(lineNetlist);
            circuit.AddComponent(component);
            NetType netType;
            foreach(var pin in component.Pins)
            {
                if(!circuit.Nets.Any(existingNet => existingNet.Name == pin.Node))
                {
                    netType = ClassifyNet(pin.Node);
                    Net net = new Net(pin.Node, netType);
                    circuit.AddNet(net);
                }


            }
            
        }
        return circuit;

    }




}
