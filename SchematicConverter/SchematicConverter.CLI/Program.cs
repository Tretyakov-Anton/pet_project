using SchematicConverter.Core;
namespace SchematicConverter.CLI;

class Program
{
    static void Main(string[] args)
    {
       string text = @"*test
       R1 OUT N017 .33
        R2 N012 OUT .33
        C1 OUT N016 100n
        R3 N016 0 10
        R4 N010 N009 470
        R5 N020 N019 470
        V1 +V 0 50
        V2 0 -V 50
       ";

       INetlistParser parser = new LTSpiceParser();
       Circuit circuit = parser.Parse(text);

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
