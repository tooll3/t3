using System;
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
            var sourceNamespace = node.GetAsString();
            if (!TryGetSourceAndTargetProjects(sourceNamespace, newNamespace, out var sourcePackage, out var targetPackage))
                return;

            sourcePackage.RenameNamespace(sourceNamespace, newNamespace, targetPackage);
            T3Ui.Save(false);
        }

        public static void UpdateNamespace(Guid symbolId, string nameSpace)
        {
            var symbol = SymbolRegistry.Entries[symbolId];
            var currentNamespace = symbol.Namespace;
            if (!TryGetSourceAndTargetProjects(currentNamespace, nameSpace, out var sourcePackage, out var targetPackage))
                return;

            sourcePackage.ChangeNamespaceOf(symbolId, null, nameSpace, targetPackage);
        }

        private static bool TryGetSourceAndTargetProjects(string sourceNamespace, string targetNamespace, out EditableSymbolProject sourcePackage,
                                                          out EditableSymbolProject targetPackage)
        {
            if (!TryGetEditableProjectOfNamespace(sourceNamespace, out sourcePackage))
            {
                EditorUi.Instance.ShowMessageBox($"$The namespace {sourceNamespace} was not found. This is probably a bug. " +
                                                 $"Please try to reproduce this and file a bug report with reproduction steps.",
                                                 "Could not rename namespace");
                targetPackage = null;
                return false;
            }

            if (!TryGetEditableProjectOfNamespace(targetNamespace, out targetPackage))
            {
                EditorUi.Instance.ShowMessageBox($"$The namespace {targetNamespace} belongs to a readonly project or the project does not exist.","Could not rename namespace");
                return false;
            }

            return true;

            static bool TryGetEditableProjectOfNamespace(string targetNamespace, out EditableSymbolProject targetProject)
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
        }

        public static void UpdateChangedOperators()
        {
            foreach (var package in ProjectSetup.EditableSymbolPackages)
                package.RecompileIfNecessary();
        }

        private readonly record struct PackageNamespaceInfo(EditableSymbolProject Project, string RootNamespace);
    }
}