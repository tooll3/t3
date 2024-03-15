using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Compilation;
using T3.Editor.Gui;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Windows;
using T3.Editor.SystemUi;

namespace T3.Editor.UiModel
{
    /// <summary>
    /// And editor functionality that handles the c# compilation of symbol classes.
    /// </summary>
    internal partial class EditableSymbolProject
    {
        /// <summary>
        /// this currently is primarily used when re-ordering symbol inputs and outputs
        /// </summary>
        public static bool UpdateSymbolWithNewSource(Symbol symbol, string newSource)
        {
            if (CheckCompilation())
                return false;

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
            if (CheckCompilation())
                return;
            
            var sourceNamespace = node.GetAsString();
            if (!TryGetSourceAndTargetProjects(sourceNamespace, newNamespace, out var sourcePackage, out var targetPackage))
                return;

            sourcePackage.RenameNamespace(sourceNamespace, newNamespace, targetPackage);
            T3Ui.Save(false);
        }

        public static void ChangeSymbolNamespace(Guid symbolId, string newNamespace)
        {
            if (CheckCompilation())
                return;
            
            var symbol = SymbolRegistry.Entries[symbolId];
            var command = new ChangeSymbolNamespaceCommand(symbol, newNamespace, ChangeNamespace);
            UndoRedoStack.AddAndExecute(command);
            return;

            static void ChangeNamespace(Guid symbolId, string nameSpace)
            {
                var symbol = SymbolRegistry.Entries[symbolId];
                var currentNamespace = symbol.Namespace;
                if (currentNamespace == nameSpace)
                    return;

                if (!TryGetSourceAndTargetProjects(currentNamespace, nameSpace, out var sourceProject, out var targetProject))
                    return;

                sourceProject.ChangeNamespaceOf(symbolId, nameSpace, targetProject);
            }
        }

        private static bool TryGetSourceAndTargetProjects(string sourceNamespace, string targetNamespace, out EditableSymbolProject sourceProject,
                                                          out EditableSymbolProject targetProject)
        {
            if (!TryGetEditableProjectOfNamespace(sourceNamespace, out sourceProject))
            {
                EditorUi.Instance.ShowMessageBox($"The namespace {sourceNamespace} was not found. This is probably a bug. " +
                                                 $"Please try to reproduce this and file a bug report with reproduction steps.",
                                                 "Could not rename namespace");
                targetProject = null;
                return false;
            }

            if (!TryGetEditableProjectOfNamespace(targetNamespace, out targetProject))
            {
                EditorUi.Instance.ShowMessageBox($"The namespace {targetNamespace} belongs to a readonly project or the project does not exist." +
                                                 $"Try making a new project with your desired namespace","Could not rename namespace");
                return false;
            }

            return true;

            static bool TryGetEditableProjectOfNamespace(string targetNamespace, out EditableSymbolProject targetProject)
            {
                var namespaceInfos = ProjectSetup.EditableSymbolProjects
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

        public static void RecompileChangedProjects()
        {
            if (_recompiling)
                return;

            while (RecompiledProjects.TryDequeue(out var project))
            {
                project.UpdateSymbols();
            }
            
            bool needsRecompilation = false;
            foreach (var package in ProjectSetup.EditableSymbolProjects)
            {
                needsRecompilation |= package.NeedsCompilation;
            }
            
            if(!needsRecompilation)
                return;
            
            _recompiling = true;
            
            Task.Run(() =>
            {
                foreach (var package in ProjectSetup.EditableSymbolProjects)
                {
                    if (package.NeedsCompilation)
                    {
                        if (package.TryRecompile())
                        {
                            RecompiledProjects.Enqueue(package);
                        }
                    }
                }
                
                _recompiling = false;
            });
        }

        public static bool CheckCompilation()
        {
            if (IsCompiling)
            {
                Log.Error("Compilation is in progress - no modifications can be applied until it is complete.");
                return true;
            }

            return false;
        }

        private readonly record struct PackageNamespaceInfo(EditableSymbolProject Project, string RootNamespace);
        private static volatile bool _recompiling;
        private static readonly ConcurrentQueue<EditableSymbolProject> RecompiledProjects = new();
        public static bool IsCompiling => _recompiling;
    }
}