using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Logging;
using T3.Editor.UiModel;
using T3.Serialization;
// ReSharper disable NotAccessedField.Local

namespace T3.Editor.Gui.UiHelpers.Wiki
{
    /// <summary>
    /// Exports a json string for automatically review and correction. 
    /// </summary>
    public static class ExportDocumentationStrings
    {

        public static void ExportDocumentationAsJson()
        {
            var results = new List<DocumentationEntry>();

            foreach (var symbolUi in SymbolUiRegistry.Entries.Values)
            {
                if (!string.IsNullOrWhiteSpace(symbolUi.Description))
                {
                    results.Add(new DocumentationEntry
                                    {
                                        Type = DocumentationEntry.Types.Description,
                                        Text = symbolUi.Description,
                                        SymbolId = symbolUi.Symbol.Id,
                                        Id = Guid.Empty
                                    });
                }

                foreach (var annotation in symbolUi.Annotations.Values)
                {
                    results.Add(new DocumentationEntry
                                    {
                                        Type = DocumentationEntry.Types.Annotation,
                                        Text = annotation.Title,
                                        SymbolId = symbolUi.Symbol.Id,
                                        Id = annotation.Id
                                    });
                }

                foreach (var childUi in symbolUi.ChildUis)
                {
                    if (string.IsNullOrWhiteSpace(childUi.Comment))
                        continue;

                    results.Add(new DocumentationEntry
                                    {
                                        Type = DocumentationEntry.Types.Comment,
                                        Text = childUi.Comment,
                                        SymbolId = symbolUi.Symbol.Id,
                                        Id = childUi.Id
                                    });
                }

                JsonUtils.SaveJson(results, DocumentationJsonFilename);
            }
        }
        
        
        public static void ImportDocumentationAsJson()
        {
            var results = JsonUtils.TryLoadingJson<List<DocumentationEntry>>(DocumentationJsonFilename);
            if (results == null)
            {
                Log.Warning($"Failed to load or parse {DocumentationJsonFilename}");
                return;
            }

            foreach (var r in results)
            {
                if (SymbolUiRegistry.Entries.TryGetValue(r.SymbolId, out var symbolUi))
                {
                    switch (r.Type)
                    {
                        case DocumentationEntry.Types.Description:
                            symbolUi.Description = r.Text;

                            break;
                        
                        case DocumentationEntry.Types.Annotation:
                            if (symbolUi.Annotations.TryGetValue(r.Id, out var a))
                            {
                                a.Title = r.Text;
                            }
                            break;
                        
                        case DocumentationEntry.Types.Comment:
                            var childUi= symbolUi.ChildUis.FirstOrDefault(c => c.Id == r.Id);
                            if (childUi != null)
                            {
                                childUi.Comment = r.Text;
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    symbolUi.FlagAsModified();
                }
            }
        }
        
        
        private class DocumentationEntry
        {
            public enum Types
            {
                Description,
                Annotation,
                Comment,
            }

            public Types Type;
            public string Text;
            public Guid SymbolId;
            public Guid Id;
        }
        
        private const string DocumentationJsonFilename = "documentation.json";

    }
}