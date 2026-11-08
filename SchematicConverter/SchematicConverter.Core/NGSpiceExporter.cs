using System.Text;

namespace SchematicConverter.Core;

public class NGSpiceExporter
{

    public string Export (Circuit circuit)
    {
        StringBuilder stringBuilder = new StringBuilder("Тестовый нетлист с LTSPice");
        foreach(var item in circuit.Components)
        {
            stringBuilder.Append(item.ReferenceDesignator + " ");
            foreach(var itemPin in item.Pins)
            { 
                stringBuilder.Append(itemPin.Node + " " + item.Nominal);
            }

        }
        stringBuilder.Append(".end");


        return stringBuilder.ToString();

    }



}
