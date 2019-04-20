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

        public readonly List<OutputDefinition> OutputDefinitions = new List<OutputDefinition>();

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
                CreateNewChildInstance(symbolChild, newInstance);
            }

            // connect instances
            foreach (var connection in Connections)
            {
                var sourceInstance = (from instance in newInstance.Children
                                      where instance.Id == connection.SourceChildId
                                      select instance).SingleOrDefault();

                if (sourceInstance == null)
                {
                    Debug.Assert(connection.SourceChildId == Guid.Empty);
                    sourceInstance = newInstance;
                }

                var sourceOutput = (from output in sourceInstance.Outputs
                                    where output.Id == connection.OutputDefinitionId
                                    select output).Single();

                var targetInstance = (from instance in newInstance.Children
                                      where instance.Id == connection.TargetChildId
                                      select instance).SingleOrDefault();
                if (targetInstance == null)
                {
                    Debug.Assert(connection.TargetChildId == Guid.Empty);
                    targetInstance = newInstance;
                }

                var targetInput = (from input in targetInstance.Inputs
                                   where input.Id == connection.InputDefinitionId
                                   select input).Single();

                targetInput.AddConnection(sourceOutput);
            }

            _instancesOfSymbol.Add(newInstance);

            return newInstance;
        }

        private static void CreateNewChildInstance(SymbolChild symbolChild, Instance parentInstance)
        {
            var childSymbol = symbolChild.Symbol;
            var childInstance = childSymbol.CreateInstance();
            childInstance.Id = symbolChild.Id;
            childInstance.Parent = parentInstance;

            // set up the inputs for the child instance
            for (int i = 0; i < symbolChild.Symbol.InputDefinitions.Count; i++)
            {
                Debug.Assert(i < childInstance.Inputs.Count);
                Guid inputDefinitionId = childSymbol.InputDefinitions[i].Id;
                childInstance.Inputs[i].Input = symbolChild.InputValues[inputDefinitionId];
                childInstance.Inputs[i].Id = inputDefinitionId;
            }

            // set up the outputs for the child instance
            for (int i = 0; i < symbolChild.Symbol.OutputDefinitions.Count; i++)
            {
                Debug.Assert(i < childInstance.Outputs.Count);
                childInstance.Outputs[i].Id = childSymbol.OutputDefinitions[i].Id;
            }

            parentInstance.Children.Add(childInstance);
        }

        public Guid AddChild(Symbol symbol)
        {
            var newChild = new SymbolChild(symbol);
            Children.Add(newChild);

            foreach (var instance in _instancesOfSymbol)
            {
                CreateNewChildInstance(newChild, instance);
            }

            return newChild.Id;
        }

        void DeleteInstance(Instance op)
        {
            _instancesOfSymbol.Remove(op);
        }

        public Connection GetConnectionForInput(InputDefinition input)
        {
            return Connections.FirstOrDefault(c => c.TargetChildId == input.Id);
        }

        public Connection GetConnectionForOutput(OutputDefinition output)
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

        public class OutputDefinition
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
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