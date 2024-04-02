using System;
using System.Collections.Generic;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Editor.Gui.Windows.TimeLine;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Commands.Animation
{
    public class ChangeKeyframesCommand : ICommand
    {
        public string Name => "Move keyframes";
        public bool IsUndoable => true;

        private readonly Dictionary<VDefinition, VDefinition> _originalDefForReferences = new();
        private readonly Dictionary<VDefinition, VDefinition> _newDefForReferences = new();
        private readonly IEnumerable<Curve> _curves;
        private readonly Guid _symbolId;

        public ChangeKeyframesCommand(in Guid symbolId, IEnumerable<VDefinition> vDefinitions, IEnumerable<Curve> curves)
        {
            _curves = curves;
            _symbolId = symbolId;
            foreach (var def in vDefinitions)
            {
                _originalDefForReferences[def] = def.Clone();
            }
        }

        public void StoreCurrentValues()
        {
            foreach (var referencedDefinition in _originalDefForReferences.Keys)
            {
                _newDefForReferences[referencedDefinition] = referencedDefinition.Clone();
            }
        }

        public void Undo()
        {
            foreach (var (referencedDefinition, orgDef) in _originalDefForReferences)
            {
                referencedDefinition.CopyValuesFrom(orgDef);
            }

            UpdateAllTangents();
        }


        public void Do()
        {
            foreach (var (referencedDefinition, newDef) in _newDefForReferences)
            {
                referencedDefinition.CopyValuesFrom(newDef);
            }
            
            UpdateAllTangents();
        }
        
        
        private void UpdateAllTangents()
        {
            foreach (var c in _curves)
            {
                c.UpdateTangents();
            }

            AnimationParameterEditing.CurvesTablesNeedsRefresh = true;
            SymbolUiRegistry.TryGetSymbolUi(_symbolId, out var symbolUi);
            symbolUi?.FlagAsModified();
        }
    }
}