using System;
using System.Collections.Generic;
using T3.Core.Operator;



//
// namespace T3.Gui.PresetControl
// {
//     interface IPresetController
//     {
//         void TryRemovingPreset(int deviceIndex, int presetIndex);
//         //void DuplicatePreset(int de)
//     }
//
//     public struct GridPos
//     {
//         public int PresetRow;
//         //public int Grid
//     }
//     
//     /// <summary>
//     /// Manages the creation and application of <see cref="PresetDevice"/>
//     /// </summary>
//     /// <remarks>
//     /// We cant prevent a <see cref="PresetDevice.PresetParameter"/> to unique
//     /// </remarks>
//     public class PresetLibrary
//     {
//         
//         public List<PresetDevice.PresetParameter> GetPresetParameters(Guid symbolId, Guid symbolChildId, Guid inputId)
//         {
//             // Todo: Implement
//             return new List<PresetDevice.PresetParameter>();
//         }
//
//         public List<PresetDevice.PresetParameter> GetPresetParametersForInput(SymbolChild.Input input)
//         {
//             // Todo: Implement
//             return new List<PresetDevice.PresetParameter>();
//         }
//
//         public void AddInputToCurrentDevice()
//         {
//             
//         }
//         
//         //public void 
//     }
//     
//     
//     /// <summary>
//     /// Represents either an operator symbol child or a combination of parameters of various symbol children that can be controlled.   
//     /// </summary>
//     public class PresetDevice
//     {
//         public Guid Id;
//         public int GridColumnIndex;
//         public List<PresetParameter> Parameters = new List<PresetParameter>();
//
//         /// <summary>
//         /// Points to a blendable parameter of a SymbolChild  
//         /// </summary>
//         public class PresetParameter
//         {
//             public Guid SymbolChildId;
//             public Guid SymbolId;
//             public Guid ParameterId;
//         }
//     }
//
//     
//     /// <summary>
//     /// A configuration
//     /// </summary>
//     public class ControlPreset
//     {
//         public Guid Id;
//         public int GridRowI;
//
//     }
//
//     
// }