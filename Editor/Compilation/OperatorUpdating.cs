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
        /// <summary>
        /// An event called that is called if the file hook detects change to the symbol source code.
        /// </summary>
        public static void ResourceUpdateHandler(OperatorResource resource, string path)
        {
            //Log.Info($"Operator source '{path}' changed.");
            //Log.Info($"Actual thread Id {Thread.CurrentThread.ManagedThreadId}");

            var success = resource.ParentProject.TryRecompile();

            if (success)
            {
                resource.Updated = true;
                resource.RefreshType();
            }
        }

        public static bool TryCreateSymbolFromSource(string sourceCode, string newSymbolName, Guid newSymbolId, string @namespace, CsProjectFile parentAssembly,
                                                     out Symbol newSymbol)
        {
            var newAssembly = CompileSymbolFromSource(sourceCode, newSymbolName, parentAssembly);
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

        public static bool UpdateSymbolWithNewSource(Symbol symbol, string newSource)
        {
            var newAssembly = CompileSymbolFromSource(newSource, symbol.Name, symbol.ParentAssembly);
            if (newAssembly == null)
                return false;

            //string path = @"Operators\Types\" + symbol.Name + ".cs";
            var sourcePath = symbol.SymbolPackage.BuildFilepathForSymbol(symbol, SymbolPackage.SourceCodeExtension);

            var operatorResource = EditorResourceManager.Instance.GetOperatorFileResource(sourcePath);
            if (operatorResource != null)
            {
                operatorResource.OperatorAssembly = newAssembly;
                operatorResource.Updated = true;
                symbol.PendingSource = newSource;
                return true;
            }

            Log.Error($"Could not update symbol '{symbol.Name}' because its file resource couldn't be found.");

            return false;
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
            var uiSymbolDatas = EditableSymbolPackage.SymbolDataByProject.Values.ToList();
            foreach (var uiSymbolData in uiSymbolDatas)
            {
                if (!uiSymbolData.CanRecompile)
                    continue;
                
                uiSymbolData.RenameNameSpace(node, nameSpace);
            }
        }
    }
}