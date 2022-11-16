using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Operators.Types.Id_a256d70f_adb3_481d_a926_caf35bd3e64c;

namespace T3.Editor.Gui.Templates
{
    public class TemplateDefinition
    {
        public delegate void SetupAction(Instance newInstance, string symbolName, string nameSpace, string description, string resourceFolder);

        public string Title;
        public string DefaultSymbolName;
        public string Summary;
        public string Documentation;
        public Guid TemplateSymbolId;
        public SetupAction AfterSetupAction = null;

        public static readonly List<TemplateDefinition> TemplateDefinitions
            = new()
                  {
                      new TemplateDefinition
                          {
                              Title = "Empty Project",
                              DefaultSymbolName = "NewProject",
                              Summary = "Creates a new project and sets up a folder structure for your resources.",
                              Documentation =
                                  "It will create a new Symbol and setup a folder structure for project related files like your soundtrack or images.",
                              TemplateSymbolId = Guid.Parse("fe8aeb9b-61ac-4a0e-97ee-4833233ac9d1"),
                              AfterSetupAction = (newChildUi, name, nameSpace, description, resourceFolder) =>
                                                 {
                                                     Directory.CreateDirectory(Path.Combine(resourceFolder, "soundtrack"));
                                                     Directory.CreateDirectory(Path.Combine(resourceFolder, "images"));
                                                     Directory.CreateDirectory(Path.Combine(resourceFolder, "geometry"));
                                                 }
                          },
                      new TemplateDefinition
                          {
                              Title = "3d Project",
                              DefaultSymbolName = "New3dProject",
                              Summary = "Something else",
                              Documentation =
                                  "This will create a new Symbol with a basic template to get you started. It will also setup a folder structure for project related files like soundtrack or images.",
                              TemplateSymbolId = Guid.Parse("38fd2e32-53f6-49ce-9aa7-28f3ac433dd8"),
                          },

                      new TemplateDefinition
                          {
                              Title = "Point Compute Shader",
                              DefaultSymbolName = "NewPointShader",
                              Summary = "Creates a compute shader setup",
                              Documentation =
                                  "This will create a new Symbol with an fully working compute shader example. It will setup and open the hlsl shader source code.",
                              TemplateSymbolId = Guid.Parse("0db659a4-d0ba-4d23-acac-aea5ba5b57dc"),
                              AfterSetupAction = (newInstance, name, nameSpace, description, resourceFolder) =>
                                                 {
                                                     Directory.CreateDirectory(Path.Combine(resourceFolder, "shader"));

                                                     // Duplicate and assign new shader source file
                                                     try
                                                     {
                                                         var newShaderFilename = $@"{resourceFolder}\shader\{name}.hlsl";
                                                         var shaderInstance = newInstance.Children.SingleOrDefault(c => c.Symbol.Id ==
                                                             Guid.Parse("a256d70f-adb3-481d-a926-caf35bd3e64c"));

                                                         File.Copy(@"Resources\examples\templates\PointShaderTemplate.hlsl",
                                                                   newShaderFilename);

                                                         if (shaderInstance is ComputeShader computeShader)
                                                         {
                                                             computeShader.Source.TypedInputValue.Value = newShaderFilename;
                                                             computeShader.Source.DirtyFlag.Invalidate();
                                                             computeShader.Source.Input.IsDefault = false;
                                                         }
                                                         else
                                                         {
                                                             Log.Warning("Can't find compute shader for source file");
                                                         }

                                                         // Open editor
                                                         Process.Start(new ProcessStartInfo(newShaderFilename) { UseShellExecute = true });
                                                     }
                                                     catch (Win32Exception e)
                                                     {
                                                         Log.Warning("Assigning new shader failed: " + e.Message);
                                                     }
                                                 }
                          },
                  };
    }
}