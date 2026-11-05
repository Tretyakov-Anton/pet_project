
namespace SchematicConverter.Core;

public class Pin
{
    public int Number{get; }
    
    public string Node { get; }

    public Pin(int numberPin, string nodePin)
    {
       ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(numberPin, 0);
       ArgumentException.ThrowIfNullOrWhiteSpace(nodePin);
       Number = numberPin;
       Node = nodePin;
    }

}
