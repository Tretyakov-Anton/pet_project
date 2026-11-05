    namespace SchematicConverter.Core;

    public class Circuit
    {

        private List<ComponentInstance> _components = new List<ComponentInstance>();
        private List<Net> _net = new List<Net>();

        public IReadOnlyList<ComponentInstance> Components {get => _components; }
        public IReadOnlyList<Net> Nets {get => _net;}
        public void AddComponent(ComponentInstance component)
        {
            ArgumentNullException.ThrowIfNull(component);
            if(_components.Any(u => u.ReferenceDesignator == component.ReferenceDesignator))
            {
                throw new InvalidOperationException($"Компонент {component.ReferenceDesignator} уже существует в схеме.");
            }
            _components.Add(component);
        }

        public void AddNet(Net net)
        {
            ArgumentNullException.ThrowIfNull(net);
            if(_net.Any(u => u.Name == net.Name))
            {
                throw new InvalidOperationException($"Цепь {net.Name} уже существует в схеме.");
            }
            _net.Add(net);
        }      
    } 

