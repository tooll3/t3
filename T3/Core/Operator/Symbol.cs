using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
//using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace T3.Core.Operator
{
    /// <summary>
    /// Represents the definition of an operator. It can include:
    /// - <see cref="SymbolChild"/>s that references other Symbols
    /// - <see cref="Connection"/>s that connect these children
    /// </summary>
    /// <remarks>
    /// - There can be multiple <see cref="Instance"/>s of a symbol.
    /// </remarks>
    public class Symbol : IDisposable
    {
        public Guid Id { get; set; }
        public string SourcePath { get; set; }
        public string SymbolName { get; set; }
        public string Namespace { get; set; }

        private readonly List<Instance> _instancesOfSymbol = new List<Instance>();
        public readonly List<SymbolChild> Children = new List<SymbolChild>();
        public readonly List<Connection> Connections = new List<Connection>();

        /// <summary>
        /// Inputs of this symbol. input values are the default values (exist only once per symbol)
        /// </summary>
        public readonly List<InputDefinition> InputDefinitions = new List<InputDefinition>();

        public Type InstanceType { get; set; }


        #region public API =======================================================================

        public void Dispose()
        {
            _instancesOfSymbol.ForEach(instance => instance.Dispose());
        }

        public Instance CreateInstance()
        {
            var newInstance = Activator.CreateInstance(InstanceType) as Instance;
            Debug.Assert(newInstance != null);
            newInstance.Symbol = this;

            // create child instances
            foreach (var symbolChild in Children)
            {
                var childInstance = symbolChild.Symbol.CreateInstance();
                childInstance.Id = symbolChild.Id;
                childInstance.Parent = newInstance;

                // set up the inputs for the child instance
                for (int i = 0; i < InputDefinitions.Count; i++)
                {
                    Debug.Assert(i < newInstance.Inputs.Count);
                    if (newInstance.Inputs[i] is IInputSlot inputSlot)
                    {
                        inputSlot.Input = symbolChild.InputValues[InputDefinitions[i].Id];
                    }
                }

                newInstance.Children.Add(childInstance);
            }

            _instancesOfSymbol.Add(newInstance);

            return newInstance;
        }

        public Guid AddChild(Symbol symbol)
        {
            var newChild = new SymbolChild(symbol);
            Children.Add(newChild);

            foreach (var instance in _instancesOfSymbol)
            {
                var childInstance = symbol.CreateInstance();
                childInstance.Id = newChild.Id;
                childInstance.Parent = instance;

                instance.Children.Add(childInstance);
            }

            return newChild.Id;
        }

        void DeleteInstance(Instance op)
        {
            _instancesOfSymbol.Remove(op);
        }

        public Symbol.Connection GetConnectionForInput(Symbol.InputDefinition input)
        {
            return Connections.FirstOrDefault(c => c.TargetChildId == input.Id);
        }

        // Fix me: Use OutputDefinition once it's available
        public Symbol.Connection GetConnectionForOutput(Symbol.InputDefinition output)
        {
            return Connections.FirstOrDefault(c => c.SourceChildId == output.Id);
        }

        #endregion

        #region sub classses =============================================================================

        /// <summary>
        /// Options on the visual presentation of <see cref="Symbol"/> input.
        /// </summary>
        public class InputDefinition
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public InputValue DefaultValue { get; set; }

            public Relevancy Relevance;
            public enum Relevancy
            {
                Required,
                Relevant,
                Optional
            }

            //TODO: how do we handle MultiInputs?
        }

        public class Connection
        {
            public Guid SourceChildId { get; }
            public Guid OutputDefinitionId { get; }
            public Guid TargetChildId { get; }
            public Guid InputDefinitionId { get; }

            public Connection(Guid sourceChildId, Guid outputDefinitionId, Guid targetChildId, Guid inputDefinitionId)
            {
                SourceChildId = sourceChildId;
                OutputDefinitionId = outputDefinitionId;
                TargetChildId = targetChildId;
                InputDefinitionId = inputDefinitionId;
            }
        }
        #endregion
    }
}