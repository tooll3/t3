using ImGuiNET;

namespace T3.Editor.Gui.UiHelpers;

/// <summary>
/// Framework for rendering modal dialogs.
///
/// Should be implemented like...
///
/// void SomeClass {
///     void Draw() {
///         _someDialog.Draw();
/// 
///        if(ImGui.Button()) {
///            _someDialog.ShowNextFrame();
///        }
///     }
/// }
/// 
/// void SomeDialog()
/// {
///     void Draw()
///     {
///         if(BeginDialog("myTitle"))
///         {    
/// 
///           // draw your content...
/// 
///           EndDialogContent();
///         }   
///         EndDialog();
///     }
/// }
/// 
/// </summary>
public abstract class ModalDialog
{
    internal void ShowNextFrame()
    {
        _shouldShowNextFrame = true;
        OnShowNextFrame();
    }
        
    // Todo - should be an abstract function other modal dialogs can use to initialize their values
    protected virtual void OnShowNextFrame(){}

    protected bool BeginDialog(string title)
    {
        if (_shouldShowNextFrame)
        {
            _shouldShowNextFrame = false;
            ImGui.OpenPopup(title);
        }


        ImGui.SetNextWindowSize(DialogSize * T3Ui.UiScaleFactor, ImGuiCond.Appearing);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(Padding, Padding));

        bool isOpen = true;
        if (!ImGui.BeginPopupModal(title, ref isOpen, ImGuiWindowFlags.Popup | Flags ))
            return false;
            
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ItemSpacing);
        FrameStats.Current.OpenedPopUpName = title;
        return true;
    }

    /// <summary>
    /// Only call if BeginDialog returned true
    /// </summary>
    protected static void EndDialogContent()
    {
        ImGui.PopStyleVar();
        ImGui.EndPopup();
    }

    /// <summary>
    /// Call always
    /// </summary>
    protected static void EndDialog()
    {
        ImGui.PopStyleVar();
    }

    private bool _shouldShowNextFrame;
    protected Vector2 DialogSize = new(500, 250);
    protected Vector2 ItemSpacing = new(4, 10);
    protected float Padding = 20;
    protected ImGuiWindowFlags Flags = ImGuiWindowFlags.None;
}