using System;
using System.Numerics;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5b127401_600c_4247_9d59_2f6ff359ba85
{
    public class _GetSceneDefinitionPoints : Instance<_GetSceneDefinitionPoints>
    {
        // [Output(Guid = "8a1e3bc8-a7bd-40b5-a4cf-241c13bddbfb")]
        // public readonly Slot<Command> Output = new();
        
        [Output(Guid = "D9C04756-8922-496D-8380-120F280EF65B")]
        public readonly Slot<StructuredList> ResultList = new();

        
        public _GetSceneDefinitionPoints()
        {
            ResultList.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var sceneDefinition = SceneSetup.GetValue(context);
            if(sceneDefinition== null)
                return;
            
            sceneDefinition.GenerateSceneDrawDispatches();
            
            if(sceneDefinition.Dispatches.Count == 0)
                return;
            
            //var rot = Quaternion.CreateFromAxisAngle(Vector3.Normalize( RotationAxis.GetValue(context)), RotationAngle.GetValue(context) * MathUtils.ToRad);
            //var array = _addSeparator ? _pointListWithSeparator : _pointList;
            var array = new StructuredList<Point>(sceneDefinition.Dispatches.Count);
            
            for (var index = 0; index < sceneDefinition.Dispatches.Count; index++)
            {
                var sceneDispatch = sceneDefinition.Dispatches[index];
                var matrix = sceneDispatch.CombinedTransform;
                
                //OutPosition.Value = pos;
                array.TypedElements[index].Position = new Vector3(matrix.M41, matrix.M42, matrix.M43);
                array.TypedElements[index].W = 1;
                array.TypedElements[index].Color = Vector4.One;
                array.TypedElements[index].Stretch = new Vector3( MathF.Abs(matrix.M11), MathF.Abs(matrix.M22), MathF.Abs(matrix.M33));
                array.TypedElements[index].Selected = 1;
                array.TypedElements[index].Orientation = Quaternion.CreateFromRotationMatrix(matrix);
                //Log.Debug("Dispatch: " + sceneDispatch.CombinedTransform);
            }
            
            ResultList.Value = array;            
            
        }
        
        private readonly StructuredList<Point> _pointList = new(1);
        

        [Input(Guid = "41054d35-5564-42db-9109-263f8c447057")]
        public readonly InputSlot<SceneSetup> SceneSetup = new();

        
    }
}