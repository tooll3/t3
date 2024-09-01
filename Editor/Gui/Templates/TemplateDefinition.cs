using System.IO;
using T3.Core.Operator;
using T3.Editor.Gui.Windows;

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
        
        public static bool RemoveTemplateDefinition(TemplateDefinition templateDefinition)
        {
            return TemplateDefinitionsEditable.Remove(templateDefinition);
        }

        public static IReadOnlyList<TemplateDefinition> TemplateDefinitions => TemplateDefinitionsEditable;

        // Todo - move these defaults to the lib/examples projects and instead of parsing a guid, use a method to get the guid using reflection
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
                  };
    }
}