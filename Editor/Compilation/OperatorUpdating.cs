using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui;
using T3.Editor.Gui.Windows;
using T3.Editor.SystemUi;
using T3.Editor.UiModel;

namespace T3.Editor.Compilation
{
    /// <summary>
    /// And editor functionality that handles the c# compilation of symbol classes.
    /// </summary>
    internal static class OperatorUpdating
    {

        // this currently is primarily used when re-ordering symbol inputs and outputs
        public static bool UpdateSymbolWithNewSource(Symbol symbol, string newSource)
        {
            if (!symbol.SymbolPackage.IsModifiable)
            {
                Log.Error($"Could not update symbol '{symbol.Name}' because it is not modifiable.");
                return false;
            }

            var editableSymbolPackage = (EditableSymbolProject)symbol.SymbolPackage;
            return editableSymbolPackage.TryRecompileWithNewSource(symbol, newSource);
        }

        public static void RenameNameSpaces(NamespaceTreeNode node, string newNamespace)
        {
            string sourceNamespace = node.GetAsString();
            if (!IsEditableTargetNamespace(sourceNamespace, out var sourcePackage))
            {
                EditorUi.Instance.ShowMessageBox("Could not rename namespace", "The source namespace belongs to a readonly project.");
                return;
            }
            
            if(!IsEditableTargetNamespace(newNamespace, out var targetNamespace))
            {
                EditorUi.Instance.ShowMessageBox("Could not rename namespace", "The target namespace belongs to a readonly project.");
                return;
            }
            
            sourcePackage.RenameNameSpace(sourceNamespace, newNamespace, targetNamespace);
            T3Ui.Save(false);
        }

        private static bool IsEditableTargetNamespace(string targetNamespace, out EditableSymbolProject targetProject)
        {
            var namespaceInfos = ProjectSetup.EditableSymbolPackages
                                                     .Select(package => new PackageNamespaceInfo(package, package.CsProjectFile.RootNamespace));

            foreach (var namespaceInfo in namespaceInfos)
            {
                var projectNamespace = namespaceInfo.RootNamespace;

                if (projectNamespace == null)
                    continue;

                if (targetNamespace.StartsWith(projectNamespace))
                {
                    targetProject = namespaceInfo.Project;
                    return true;
                }
            }

            targetProject = null;
            return false;
        }
        
        private readonly record struct PackageNamespaceInfo(EditableSymbolProject Project, string RootNamespace);

        public static void UpdateChangedOperators()
        {
            foreach (var package in ProjectSetup.EditableSymbolPackages)
                package.RecompileIfNecessary();
        }
    }
}