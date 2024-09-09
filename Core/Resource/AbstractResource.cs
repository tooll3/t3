namespace T3.Core.Resource
{
    public abstract class AbstractResource
    {
        protected AbstractResource()
        {
        }
        
        protected AbstractResource(uint id, string name)
        {
            Id = id;
            Name = name;
        }

        public uint Id { get; init; }
        public string Name { get => _name; init => _name = value; }

        private string _name;
        
        protected void UpdateName(string name) => _name = name;
    }
}