
namespace SchematicConverter.Core;

public class Pin
{
    public int NumberPin{get; }
    
    public string NodePin { get; }

    public Pin(int numberPin, string nodePin)
    {
       ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(numberPin, 0);
       ArgumentException.ThrowIfNullOrWhiteSpace(nodePin);
       NumberPin = numberPin;
       NodePin = nodePin;
    }

}
