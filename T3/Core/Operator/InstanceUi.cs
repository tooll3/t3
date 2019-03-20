using System.Numerics;

namespace T3.Core.Operator
{
    /// <summary>
    /// Properties needed for visual representation of an instance. Should later be moved to gui component.
    /// </summary>
    public class InstanceUi
    {
        public Instance Instance;
        public Vector2 Position = Vector2.Zero;
        public Vector2 Size = new Vector2(100, 30);
        public bool Visible = true;
        public bool Selected = false;
        public string Name { get; set; } = string.Empty;
        public string ReadableName
        {
            get
            {
                return string.IsNullOrEmpty(Name) ? Instance.Symbol.SymbolName : Name;
            }
        }
    }
}