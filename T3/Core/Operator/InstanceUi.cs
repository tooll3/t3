using System.Numerics;

namespace T3.Core.Operator
{
    /// <summary>
    /// Properties needed for visual representation of an instance. Should later be moved to gui component.
    /// </summary>
    public class InstanceUi
    {
        public Vector2 Position = Vector2.Zero;
        public Vector2 Size = new Vector2(100, 30);
        public bool Visible = true;
        public bool Selected = false;
        public Instance Instance;
    }
}