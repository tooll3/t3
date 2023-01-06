using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ImGuiNET;
using T3.Core.Logging;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

// ReSharper disable PossibleMultipleEnumeration

namespace T3.Editor.Gui.Windows
{
    public class UtilitiesWindow : Window
    {
        public UtilitiesWindow()
        {
            Config.Title = "Utilities";
        }

        protected override void DrawContent()
        {
            CustomComponents.DrawStringParameter("SvgFile", ref _svgFilePath, "not-a-font.svg", null, FileOperations.FilePickerTypes.File);
            if (CustomComponents.DisablableButton("Convert To SvgFont", File.Exists(_svgFilePath)))
            {
                Log.Debug("here");
                ConvertSvgToSvgFont(_svgFilePath);
            }
        }

        private static readonly XNamespace _ns = "http://www.w3.org/2000/svg";

        private void ConvertSvgToSvgFont(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            var xDoc = XDocument.Load(filePath);
            //const string ns = "http://www.w3.org/2000/svg";

            var svg = GetSingleElement(xDoc, _ns + "svg");
            if (svg == null)
                return;

            var defs = new XElement(_ns + "defs");
            svg.Add(defs);

            var fontDef = new XElement(_ns + "font");
            fontDef.SetAttributeValue("horiz-adv-x", 50);   // TODO: set correctly
            defs.Add(fontDef);

            var fontFace = new XElement(_ns + "font-face");
            fontFace.SetAttributeValue("font-family", "Hershey Sans 1-stroke");
            fontFace.SetAttributeValue("units-per-em", "1000");
            fontFace.SetAttributeValue("ascent", "800");
            fontFace.SetAttributeValue("descent", "-200");
            fontFace.SetAttributeValue("cap-height", "500");
            fontFace.SetAttributeValue("x-height", "300");
            fontDef.Add(fontFace);

            var glyphGroups = svg
                             .Elements(_ns + "g")?
                             .Where(c => AttrEndsWith(c, "-Font"))
                             .Elements(_ns + "g");

            foreach (var g in glyphGroups)
            {
                var frameRect = GetSingleElement(g, _ns + "rect");
                if (frameRect == null)
                    continue;

                var path = GetSingleElement(g, _ns + "path");
                if (path == null)
                    continue;

                var xTransform = GetTranslateOrDefault(frameRect);
                var width = GetFloatAttribute(frameRect, "width");
                var height = GetFloatAttribute(frameRect, "height");

                var d = path.Attribute("d")?.Value;
                if (d == null)
                    continue;

                var id = g.Attribute("id")?.Value;
                if (id == null)
                    continue;

                if (id.Length != 1)
                {
                    Log.Debug($"Skipping group with incorrect Id {id}");
                    continue;
                }
                
                var newGlyph = new XElement(_ns + "glyph");
                newGlyph.SetAttributeValue("horiz-adv-x", width);
                newGlyph.SetAttributeValue("vert-origin-x", xTransform);
                newGlyph.SetAttributeValue("unicode", id);
                newGlyph.SetAttributeValue("glyph-name", id);
                newGlyph.SetAttributeValue("d", d);

                frameRect.Remove();
                path.Remove();

                fontDef.Add(newGlyph);
                //g.Remove();
            }

            xDoc.Save(filePath + "-font.svg");

            Log.Debug("" + glyphGroups);
        }

        private static XElement GetSingleElement(XContainer xElement, XName xName)
        {
            var elements = xElement.Elements(xName);
            return elements.Count() == 1
                       ? elements.First()
                       : null;
        }

        private static float GetTranslateOrDefault(XElement frameRect, float @default = 0)
        {
            var transformString = (string)frameRect.Attribute("transform");

            if (transformString == null)
                return @default;

            var result = Regex.Match(transformString, @"translate\((.*?)\)");
            if (!result.Success)
                return @default;

            return float.TryParse(result.Groups[1].Value, out var xx) ? xx : @default;
        }

        private static float GetFloatAttribute(XElement frameRect, string attributeName, float defaultValue = 0)
        {
            var valueString = frameRect.Attribute(attributeName);
            if (valueString == null)
                return defaultValue;

            return float.TryParse(valueString.Value, out var f) ? f : defaultValue;
        }

        private static bool AttrEndsWith(XElement c, string suffix)
        {
            var attr = c.Attribute("id");
            return attr?.Value != null && attr.Value.EndsWith(suffix);
        }

        private static string _svgFilePath;

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }
    }
}