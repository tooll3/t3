using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Styling;
// ReSharper disable AccessToDisposedClosure

namespace T3.Editor.Gui.Graph.Dialogs;

public static class TypeSelector
{
    public static void Draw(ref Type selectedType)
    {
        if (!_initialized)
        {
            _typeList = TypeNameRegistry.Entries.ToArray();
            _initialized = true;
        }
        
        using var enumerator = TypeNameRegistry.Entries.GetEnumerator();
        var typeChanged = SearchableDropDown.Draw(ref _selectedTypeIndex,
                                                  SelectedTypeName,
                                                  (filter, isSelected) =>
                                                  {
                                                      if (!enumerator.MoveNext())
                                                          return SearchableDropDown.ItemResults.Completed;

                                                      var (type, typeName) = enumerator.Current;
                                                      var isMatch = false;
                                                      isMatch |= typeName.Contains(filter, StringComparison.InvariantCultureIgnoreCase);
                                                      if (_synonyms.TryGetValue(typeName, out var synonymsForType))
                                                      {
                                                          foreach (var s in synonymsForType)
                                                          {
                                                              isMatch |= s.Contains(filter, StringComparison.InvariantCultureIgnoreCase);
                                                          }
                                                      }

                                                      if (!isMatch)
                                                          return SearchableDropDown.ItemResults.FilteredOut;

                                                      if (!TypeUiRegistry.Entries.TryGetValue(type, out var properties))
                                                      {
                                                          Log.Warning($"Type {type} is missing Type properties");
                                                          return SearchableDropDown.ItemResults.FilteredOut;
                                                      }

                                                      var typeColor = ColorVariations.OperatorLabel.Apply(properties.Color).Rgba;
                                                      ImGui.PushStyleColor(ImGuiCol.Text, typeColor);

                                                      ImGui.PushStyleColor(ImGuiCol.HeaderHovered, UiColors.BackgroundActive.Fade(0.2f).Rgba);
                                                      ImGui.Selectable("##", isSelected);
                                                      var clicked = ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
                                                      ImGui.SetCursorScreenPos(ImGui.GetItemRectMin() + new Vector2(4, 4));
                                                      ImGui.TextUnformatted(typeName);
                                                      ImGui.SameLine();
                                                      ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.3f * ImGui.GetStyle().Alpha);
                                                      ImGui.TextUnformatted(" - " + type.Namespace);
                                                      ImGui.PopStyleVar();
                                                      ImGui.PopStyleColor(2);

                                                      return clicked
                                                                 ? SearchableDropDown.ItemResults.Activated
                                                                 : SearchableDropDown.ItemResults.Visible;
                                                  });

        if (typeChanged)
        {
            selectedType = SelectedType;
            //SelectedType = TypeNameRegistry.Entries.Keys.ToList()[_selectedTypeIndex];
            Log.Debug("Modified! " + SelectedTypeName);
        }
    }
    
    private static int _selectedTypeIndex;
    private static readonly Dictionary<string, string[]> _synonyms = new()
                                                                         {
                                                                             { "float", new[] { "Single", } },
                                                                             { "Texture2d", new[] { "Image", } },
                                                                             { "Vector4", new[] { "Color", "Quaternion" } },
                                                                         };
    private static Type SelectedType => IsSelectedTypeIndexValid ? _typeList[_selectedTypeIndex].Key : null;
    private static string SelectedTypeName => IsSelectedTypeIndexValid ? _typeList[_selectedTypeIndex].Value : "Unknown";
    private static bool IsSelectedTypeIndexValid => _selectedTypeIndex >= 0 && _selectedTypeIndex < _typeList.Length;

    private static KeyValuePair<Type, string>[] _typeList;
    private static bool _initialized;

}