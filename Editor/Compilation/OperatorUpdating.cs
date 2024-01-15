using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
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

        public static void RenameNameSpaces(NamespaceTreeNode node, string nameSpace)
        {
            if (!IsEditableTargetNamespace(node, out var targetPackage))
            {
                EditorUi.Instance.ShowMessageBox("Could not rename namespace", "The namespace belongs to a readonly project.");
                return;
            }

            foreach (var package in ProjectSetup.EditableSymbolPackages)
            {
                package.RenameNameSpace(node, nameSpace, targetPackage);
            }
        }

        private static bool IsEditableTargetNamespace(NamespaceTreeNode node, out EditableSymbolProject targetProject)
        {
            var namespaceInfos = ProjectSetup.EditableSymbolPackages
                                                     .Select(package => new PackageNamespaceInfo(package, package.CsProjectFile.RootNamespace));

            string targetNamespace = node.GetAsString();
            foreach (var namespaceInfo in namespaceInfos)
            {
                string projectNamespace = namespaceInfo.RootNamespace;

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

        /// <summary>
        /// A sort of hacky way to update all projects without considering their dependencies.
        /// Any update that fails will be retried until it succeeds or no more progress can be made.
        /// Should be replaced with actual dependency resolution.
        /// </summary>
        public static void UpdateChangedOperators()
        {
            foreach (var project in ProjectSetup.EditableSymbolPackages)
            {
                project.ExecutePendingUpdates();
            }
        }
    }
}