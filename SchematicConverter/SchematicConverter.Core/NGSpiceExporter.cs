using System.Text;

namespace SchematicConverter.Core;

public class NGSpiceExporter
{

    public string Export (Circuit circuit)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("* Тестовый нетлист с LTspice");
        foreach(var item in circuit.Components)
        {
            var pinsString = string.Join(" ", item.Pins.Select(p => p.Node));
            stringBuilder.AppendLine($"{item.ReferenceDesignator} {pinsString} {item.Nominal}");
        }
        stringBuilder.Append(".end");
        return stringBuilder.ToString();
    }
}
