using SchematicConverter.Core;
namespace SchematicConverter.CLI;

class Program
{
    static void Main(string[] args)
    {
       string text = @"*test
       R1 N001 VDD 10k
       C1 N002 VCC 1K
       Q123 N003 VSS 1k
       ";

        NetlistParser netlist = new NetlistParser();
        Circuit circuit = netlist.Parse(text);

        foreach(var item in circuit.Components)
        {
            Console.WriteLine($"Компоненты - {item.Type} {item.ReferenceDesignator}  {item.Nominal}");
        }

         foreach(var item in circuit.Nets)
        {
            Console.WriteLine($"Узлы - {item.Name} {item.Type}");
        }


    }
}
