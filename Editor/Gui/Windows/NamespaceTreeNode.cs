using T3.Core.Operator;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Windows
{
    /// <summary>
    /// A nested container that can contain further instances of <see cref="NamespaceTreeNode"/>
    /// </summary>
    public class NamespaceTreeNode
    {
        public string Name { get; private set; }
        public List<NamespaceTreeNode> Children { get; } = new();
        private NamespaceTreeNode Parent { get; }

        public NamespaceTreeNode(string name, NamespaceTreeNode parent = null)
        {
            Name = name;
            Parent = parent;
        }

        public string GetAsString()
        {
            var list = new List<string>();
            var t = this;
            while (t.Parent != null)
            {
                list.Insert(0, t.Name);
                t = t.Parent;
            }

            return string.Join(".", list);
        }

        public void Clear()
        {
            Children.Clear();
            Symbols.Clear();
        }

        public void PopulateCompleteTree()
        {
            Name = RootNodeId;
            Clear();

            foreach (var symbol in EditorSymbolPackage.AllSymbols.OrderBy(symbol => symbol.Namespace + symbol.Name))
            {
                SortInOperator(symbol);
            }
        }
        // define an action delegate that takes a Symbol and returns a bool

        
        public void PopulateCompleteTree(Predicate<Symbol> filterAction)
        {
            Name = RootNodeId;
            Clear();

            foreach (var symbol in EditorSymbolPackage.AllSymbols.OrderBy(symbol => symbol.Namespace + symbol.Name))
            {
                if(filterAction(symbol))
                    SortInOperator(symbol);
            }
        }
        
        
        public void SortInOperator(Symbol symbol)
        {
            if (symbol?.Namespace == null)
            {
                return;
            }

            var spaces = symbol.Namespace.Split('.');

            var currentNode = this;
            var expandingSubTree = false;

            foreach (var spaceName in spaces)
            {
                if (spaceName == "")
                    continue;

                if (!expandingSubTree)
                {
                    var node = currentNode.FindNodeDataByName(spaceName);
                    if (node != null)
                    {
                        currentNode = node;
                    }
                    else
                    {
                        expandingSubTree = true;
                    }
                }

                if (!expandingSubTree)
                    continue;

                var newNode = new NamespaceTreeNode(spaceName, currentNode);
                currentNode.Children.Add(newNode);
                currentNode = newNode;
            }

            currentNode.Symbols.Add(symbol);
        }

        private NamespaceTreeNode FindNodeDataByName(String name)
        {
            return Children.FirstOrDefault(n => n.Name == name);
        }

        public readonly List<Symbol> Symbols = new();
        public const string RootNodeId = "root";
    }
}