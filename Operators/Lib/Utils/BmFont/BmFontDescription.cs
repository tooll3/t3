using System.Runtime.InteropServices;
using System.Xml.Serialization;
using T3.Core.Logging;

namespace lib.Utils.BmFont
{
    public class BmFontDescription
    {
        public static BmFontDescription InitializeFromFile(string filepath)
        {
            if (_fontDescriptionForFilePaths == null || filepath == null)
                return null;

            if (_fontDescriptionForFilePaths.TryGetValue(filepath, out var font))
                return font;
            
            Font bmFont;
            try
            {
                var serializer = new XmlSerializer(typeof(Font));
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
            BmFont = bmFont;
            Padding = Paddings.FromString(bmFont.Info.Padding);
            
            foreach (var c in bmFont.Chars)
            {
                InfoForCharacter[c.Id] = c;
            }
            
            foreach (var kerning in bmFont.Kernings)
            {
                var key = (kerning.First << 16 | kerning.Second);
                var value = kerning.Amount;
                KerningForPairs[key] = value;
            }
        }

        public float GetKerning(int leftCharId, int rightCharId)
        {
            if (leftCharId == 0)
                return 0;
            
            var key = leftCharId << 16 | rightCharId;
            if (KerningForPairs.TryGetValue(key, out var kerning2))
            {
                return kerning2;
            }
            return 0;
        }

        public readonly Paddings Padding;
        public readonly Font BmFont;
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

        public struct Paddings
        {
            public static Paddings FromString(string str)
            {
                if (string.IsNullOrEmpty(str))
                    return _zeroPadding;

                var newPadding = new Paddings();
                var values = str.Split(",");
                if (values.Length != 4)
                    return _zeroPadding;
                
                newPadding.Up = float.TryParse(values[0], out var upPadding) ? upPadding : 0; 
                newPadding.Right = float.TryParse(values[1], out var rightPadding) ? rightPadding : 0;
                newPadding.Down = float.TryParse(values[2], out var downPadding) ? downPadding : 0;
                newPadding.Left = float.TryParse(values[3], out var leftPadding) ? leftPadding : 0;

                return newPadding;
            }
            
            public float Up;
            public float Right;
            public float Down;
            public float Left;

            private static readonly Paddings _zeroPadding = default;
        }
    }
}