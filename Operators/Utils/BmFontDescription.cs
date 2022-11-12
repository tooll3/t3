using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using T3.Core.Logging;
using T3.Operators.Utils.BmFont;

namespace Operators.Utils
{
    public class BmFontDescription
    {
        
        public static BmFontDescription InitializeFromFile(string filepath)
        {
            if (filepath == null)
                return null;

            if (_fontDescriptionForFilePaths.TryGetValue(filepath, out var font))
                return font;
            
            Font bmFont;

            var serializer = new XmlSerializer(typeof(Font));
            try
            {
                var stream = new FileStream(filepath, FileMode.Open);
                bmFont = (Font)serializer.Deserialize(stream);
                Log.Debug("loaded font with character count:" + bmFont.Chars.Length);
                stream.Close();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load font {filepath} " + e + "\n" + e.Message);
                return null;
            }

            var newFontDescription = new BmFontDescription(bmFont);
            _fontDescriptionForFilePaths[filepath] = newFontDescription;
            return newFontDescription;
        }

        private BmFontDescription(Font bmFont)
        {
            Font = bmFont;

            foreach (var c in bmFont.Chars)
            {
                InfoForCharacter[c.Id] = c;
            }

            foreach (var kerning in bmFont.Kernings)
            {
                var key = kerning.First << 16 | kerning.Second;
                var value = kerning.Amount;
                KerningForPairs[key] = value;
            }
        }
        
        public readonly Font Font;
        public readonly Dictionary<int, float> KerningForPairs = new();
        public readonly Dictionary<int, FontChar> InfoForCharacter = new();
        private static readonly Dictionary<string, BmFontDescription> _fontDescriptionForFilePaths = new();
        
        public enum HorizontalAligns
        {
            Left,
            Center,
            Right,
        }

        public enum VerticalAligns
        {
            Top,
            Middle,
            Bottom,
        }
        
    }
}