namespace T3.Core.Operator
{
    public abstract class InputAnnotation
    {
        public abstract void RenderImgui();
    }

    public class FloatInputAnnotation : InputAnnotation
    {
        public override void RenderImgui()
        {
        }
    }

}