using System.Numerics;
using ImGuiNET;

namespace T3.Gui.UiHelpers
{
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
        public void ShowNextFrame()
        {
            _shouldShowNextFrame = true;
        }

        protected bool BeginDialog(string title)
        {
            if (_shouldShowNextFrame)
            {
                _shouldShowNextFrame = false;
                ImGui.OpenPopup(title);
            }


            ImGui.SetNextWindowSize(DialogSize, ImGuiCond.FirstUseEver);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(20, 20));

            if (!ImGui.BeginPopupModal(title))
                return false;
            
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 10));
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
        protected Vector2 DialogSize = new Vector2(500, 250);
    }
}