namespace SchematicConverter.Core;

public class NetlistParser
{   
    public void ParseLine(string line)
    {
        var words = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
