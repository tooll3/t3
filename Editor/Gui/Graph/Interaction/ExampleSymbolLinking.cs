using T3.Core.Operator;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Interaction
{
    public static class ExampleSymbolLinking
    {
        // Todo: unlink Examples implementation from naming - this should be done in a different way via attribute, guid links, etc.
        public static void UpdateExampleLinks()
        {
            ExampleSymbolUis.Clear();

            const string exampleSuffix = "Example";
            const string examplesSuffix = "Examples";
            

            var potentialExamples = new List<(Range correspondingNameRange, SymbolUi exampleSymbol)>();

            var symbolUiCollection = EditorSymbolPackage.AllSymbolUis.ToList();
            Dictionary<int, List<Symbol>> symbolsByName = new(capacity: symbolUiCollection.Count);
            foreach (var symbolUi in symbolUiCollection)
            {
                var potentialExampleSymbol = symbolUi.Symbol;
                var name = potentialExampleSymbol.Name;
                var hashCode = name.GetHashCode();

                if (!symbolsByName.TryGetValue(hashCode, out var symbolsWithName))
                {
                    symbolsWithName = [];
                    symbolsByName.Add(hashCode, symbolsWithName);
                }
                
                symbolsWithName.Add(potentialExampleSymbol);

                var lastChar = name[^1];
                var endsWithS = lastChar == 's';
                var endsWithE = lastChar == 'e';
                if(!endsWithS && !endsWithE)
                    continue;

                var index = endsWithS 
                                ? name.IndexOf(examplesSuffix, StringComparison.Ordinal) 
                                : name.IndexOf(exampleSuffix, StringComparison.Ordinal);
                if (index < 0)
                    continue;

                potentialExamples.Add((..index, symbolUi));
            }

            foreach (var (correspondingSymbolNameRange, exampleSymbol) in potentialExamples)
            {
                var name = exampleSymbol.Symbol.Name[correspondingSymbolNameRange];
                var gotSymbol = symbolsByName.TryGetValue(name.GetHashCode(), out var symbolsWithName);

                if (!gotSymbol)
                    continue;

                foreach (var symbol in symbolsWithName)
                {
                    if (!ExampleSymbolUis.TryGetValue(symbol.Id, out var exampleList))
                    {
                        exampleList = [];
                        ExampleSymbolUis.Add(symbol.Id, exampleList);
                    }

                    exampleList.Add(exampleSymbol);
                }
            }

            Log.Debug($"Found {ExampleSymbolUis.Sum(x=> x.Value.Count)} examples for {ExampleSymbolUis.Count} operators out of {potentialExamples.Count} potential examples");
        }

        public static Dictionary<Guid, List<SymbolUi>> ExampleSymbolUis { get; } = new(50);
    }
}