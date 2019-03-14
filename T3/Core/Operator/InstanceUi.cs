using System.Numerics;

namespace T3.Core.Operator
{
    /*
     * Properties needed for visual representation of an instance. Should later be moved to gui component.
     */
    public class InstanceUi
    {
        public Vector2 Position = Vector2.Zero;
        public Vector2 Size = new Vector2(100, 30);
        public bool Visible = true;
    }
}