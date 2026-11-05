namespace SchematicConverter.Core;

public class Net
{
    public string Name {get; }

    public NetType Type {get; }

    public Net(string name, NetType type)
    {
       ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "Недопустимое значение типа соединения.");
        }
        Type = type;
        Name = name;
    }
}
