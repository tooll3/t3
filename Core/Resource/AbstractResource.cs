using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace T3.Core.Resource
{
    public abstract class AbstractResource
    {
        protected AbstractResource(uint id, string name)
        {
            Id = id;
            Name = name;
        }

        public uint Id { get; }
        public readonly string Name;
        public bool UpToDate { get; set; }
    }


}