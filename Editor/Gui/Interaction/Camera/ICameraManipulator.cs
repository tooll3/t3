namespace T3.Editor.Gui.Interaction.Camera;

internal interface ICameraManipulator
{
    public void ManipulateCamera(CameraSetup intendedSetup, double frameTime);
}