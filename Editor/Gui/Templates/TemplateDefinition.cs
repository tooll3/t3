using System;
using System.Collections.Generic;
using System.IO;
using T3.Core.Operator;

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

        public static void AddTemplateDefinition(TemplateDefinition templateDefinition)
        {
            TemplateDefinitionsEditable.Add(templateDefinition);
        }

        public static IReadOnlyList<TemplateDefinition> TemplateDefinitions => TemplateDefinitionsEditable;

        private static readonly List<TemplateDefinition> TemplateDefinitionsEditable 
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
                          }

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
                                                     // Duplicate and assign new shader source file
                                                     try
                                                     {
                                                         Directory.CreateDirectory(Path.Combine(resourceFolder, "shader"));
                                                         var newShaderFilename = $@"{resourceFolder}shader\{name}.hlsl";
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
                      new TemplateDefinition
                          {
                              Title = "Image Effects Shader",
                              DefaultSymbolName = "NewImageFxShader",
                              Summary = "Creates a pixel shader setup",
                              Documentation =
                                  "This will create a new Symbol with an fully working pixel (I.e. fragment) shader example. It will setup and open the hlsl shader source code.",
                              TemplateSymbolId = Guid.Parse("fdd58452-ecb4-458d-9f5b-9bce356d5125"),
                              AfterSetupAction = (newInstance, name, nameSpace, description, resourceFolder) =>
                                                 {
                                                     // Duplicate and assign new shader source file
                                                     try
                                                     {
                                                         Directory.CreateDirectory(Path.Combine(resourceFolder, "shader"));
                                                         var newShaderFilename = $@"{resourceFolder}shader\{name}.hlsl";
                                                         var shaderSetupInstance = newInstance.Children.SingleOrDefault(c => c.Symbol.Id ==
                                                             Guid.Parse("bd0b9c5b-c611-42d0-8200-31af9661f189"));

                                                         File.Copy(@"Resources\examples\templates\ImgFxShaderTemplate.hlsl",
                                                                   newShaderFilename);

                                                         if (shaderSetupInstance is _ImageFxShaderSetupStatic shaderSetup)
                                                         {
                                                             shaderSetup.Source.TypedInputValue.Value = newShaderFilename;
                                                             shaderSetup.Source.DirtyFlag.Invalidate();
                                                             shaderSetup.Source.Input.IsDefault = false;
                                                         }
                                                         else
                                                         {
                                                             Log.Warning("Can't find pixel shader for source file");
                                                         }

                                                         // Open editor
                                                         Process.Start(new ProcessStartInfo(newShaderFilename) { UseShellExecute = true });
                                                     }
                                                     catch (Exception e)
                                                     {
                                                         Log.Warning("Assigning new shader failed: " + e.Message);
                                                     }
                                                 }
                          },
                  };
    }
}