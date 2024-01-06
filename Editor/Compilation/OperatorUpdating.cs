using System;
using System.IO;
using System.Linq;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;

namespace T3.Editor.Compilation
{
    /// <summary>
    /// And editor functionality that handles the c# compilation of symbol classes.
    /// </summary>
    internal static class OperatorUpdating
    {
        // called from duplicate/combine 
        public static bool TryCreateSymbolFromSource(string sourceCode, string newSymbolName, Guid newSymbolId, string @namespace, EditableSymbolPackage package,
                                                     out Symbol newSymbol)
        {
            return package.TryCompile(sourceCode, newSymbolName, newSymbolId, @namespace, out newSymbol);
            if (newAssembly == null)
            {
                Log.Error("Error compiling duplicated type, aborting duplication.");
                newSymbol = null;
                return false;
            }

            var type = newAssembly.ExportedTypes.FirstOrDefault();
            if (type == null)
            {
                Log.Error("Error, new symbol has no compiled instance type");
                newSymbol = null;
                return false;
            }

            newSymbol = new Symbol(type, newSymbolId);
            newSymbol.PendingSource = sourceCode;
            newSymbol.Namespace = @namespace;
            return true;
        }

        // this currently is primarily used when re-ordering symbol inputs and outputs
        public static bool UpdateSymbolWithNewSource(Symbol symbol, string newSource)
        {
            if (!symbol.SymbolPackage.IsModifiable)
            {
                Log.Error($"Could not update symbol '{symbol.Name}' because it is not modifiable.");
                return false;
            }

            var editableSymbolPackage = (EditableSymbolPackage)symbol.SymbolPackage;
            return editableSymbolPackage.TryRecompileWithNewSource(symbol, newSource);

        }

        /// <summary>
        /// Updates symbol definition, instances and symbolUi if modification to operator source code
        /// was detected by Resource file hook.
        /// </summary>
        public static void UpdateChangedOperators()
        {
            var modifiedSymbols = OperatorResource.UpdateChangedOperatorTypes();
            foreach (var symbol in modifiedSymbols)
            {
                var uiSymbolData = (EditableSymbolPackage)symbol.SymbolPackage;
                uiSymbolData.UpdateUiEntriesForSymbol(symbol);
                symbol.CreateAnimationUpdateActionsForSymbolInstances();
            }
        }

        public static void RenameNameSpaces(NamespaceTreeNode node, string nameSpace)
        {
            if (!IsEditableTargetNamespace(node, out var targetPackage))
            {
                return;
            }

            foreach (var package in EditorInitialization.EditableSymbolPackages)
            {
                package.RenameNameSpace(node, nameSpace, targetPackage);
            }
        }


        private static bool IsEditableTargetNamespace(NamespaceTreeNode node, out EditableSymbolPackage targetPackage)
        {
            var namespaceInfos = EditorInitialization.EditableSymbolPackages
                                                     .Select(package => new PackageNamespaceInfo(package, package.CsProjectFile.RootNamespace));

            string targetNamespace = node.GetAsString();
            foreach (var namespaceInfo in namespaceInfos)
            {
                // trim initial `Operators.` out of the namespace

                string userNamespace = namespaceInfo.RootNamespace;
                if (userNamespace.StartsWith("Operators."))
                    userNamespace = userNamespace["Operators.".Length..];

                if (targetNamespace.StartsWith(userNamespace))
                {
                    targetPackage = namespaceInfo.Package;
                    return true;
                }
            }

            targetPackage = null;
            return false;
        }
        
        private readonly record struct PackageNamespaceInfo(EditableSymbolPackage Package, string RootNamespace);
    }
}