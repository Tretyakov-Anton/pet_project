namespace SchematicConverter.Core;

public class Circuit
{

    private List<ComponentInstance> _listComponent = new List<ComponentInstance>();
    private List<Net> _listNet = new List<Net>();

    public IReadOnlyList<ComponentInstance> ListComponentInstance {get => _listComponent; }
    public IReadOnlyList<Net> ListNode {get => _listNet;}

    public void AddComponent(ComponentInstance component)
    {
        ArgumentNullException.ThrowIfNull(component);
        foreach (var existingPin in _listComponent)
        {
            //throw new InvalidOperationException($"Пин с номером {pin.NumberPin} уже существует у компонента.");
        }
        _listComponent.Add(component);
    }
        
} 

