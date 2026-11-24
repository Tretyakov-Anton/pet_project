namespace SchematicConverter.Core;

public interface INetlistParser
{
    public Circuit Parse (string line);

}
