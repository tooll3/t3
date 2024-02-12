using ComputeShaderD3D = SharpDX.Direct3D11.ComputeShader;
using VertexShaderD3D = SharpDX.Direct3D11.VertexShader;
using PixelShaderD3D = SharpDX.Direct3D11.PixelShader;
using System;
using System.Xml.Serialization;

/*
Copyright (C) 2019 Antoine Guilbaud (IronPowerTga)

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
 claim that you wrote the original software. If you use this software
 in a product, an acknowledgment in the product documentation would be
 appreciated but is not required.

2. Altered source versions must be plainly marked as such, and must not be
 misrepresented as being the original software.

3. This notice may not be removed or altered from any source distribution.
*/
namespace Utils
{
    [XmlRoot("font")]
    public class Font
    {
        [XmlElement("info")]
        public FontInfo Info { get; set; }

        [XmlElement("common")]
        public FontCommon Common { get; set; }

        [XmlArray("pages")]
        [XmlArrayItem("page")]
        public FontPage[] Pages { get; set; }

        [XmlArray("chars")]
        [XmlArrayItem("char")]
        public FontChar[] Chars { get; set; }

        [XmlArray("kernings")]
        [XmlArrayItem("kerning")]
        public FontKerning[] Kernings { get; set; }
    }

    public class FontInfo
    {
        [XmlAttribute("face")]
        public string Face { get; set; }

        [XmlAttribute("size")]
        public Int32 Size { get; set; }

        [XmlAttribute("bold")]
        public Int32 Bold { get; set; }

        [XmlAttribute("italic")]
        public Int32 Italic { get; set; }

        [XmlAttribute("charset")]
        public string CharSet { get; set; }

        [XmlAttribute("stretchH")]
        public Int32 StretchH { get; set; }

        [XmlAttribute("smooth")]
        public Int32 Smooth { get; set; }

        [XmlAttribute("aa")]
        public Int32 SuperSampling { get; set; }

        [XmlAttribute("padding")]
        public string Padding { get; set; }

        [XmlAttribute("spacing")]
        public string Spacing { get; set; }

        [XmlAttribute("outline")]
        public int Outline { get; set; }
    }

    public class FontCommon
    {
        [XmlAttribute("lineHeight")]
        public Int32 LineHeight { get; set; }

        [XmlAttribute("base")]
        public Int32 Base { get; set; }

        [XmlAttribute("scaleW")]
        public Int32 ScaleW { get; set; }

        [XmlAttribute("scaleH")]
        public Int32 ScaleH { get; set; }

        [XmlAttribute("pages")]
        public Int32 Pages { get; set; }

        [XmlAttribute("packed")]
        public Int32 Packed { get; set; }

        [XmlAttribute("alphaChnl")]
        public Int32 AlphaChnl { get; set; }

        [XmlAttribute("redChnl")]
        public Int32 RedChnl { get; set; }

        [XmlAttribute("blueChnl")]
        public Int32 BlueChnl { get; set; }
    }

    public class FontPage
    {
        [XmlAttribute("id")]
        public Int32 Id { get; set; }

        [XmlAttribute("file")]
        public string File { get; set; }
    }

    public class FontChar
    {
        [XmlAttribute("id")]
        public Int32 Id { get; set; }

        [XmlAttribute("x")]
        public Int32 X { get; set; }

        [XmlAttribute("y")]
        public Int32 Y { get; set; }

        [XmlAttribute("width")]
        public Int32 Width { get; set; }

        [XmlAttribute("height")]
        public Int32 Height { get; set; }

        [XmlAttribute("xoffset")]
        public Int32 XOffset { get; set; }

        [XmlAttribute("yoffset")]
        public Int32 YOffset { get; set; }

        [XmlAttribute("xadvance")]
        public Int32 XAdvance { get; set; }

        [XmlAttribute("page")]
        public Int32 Page { get; set; }

        [XmlAttribute("chnl")]
        public Int32 Channel { get; set; }
    }

    public class FontKerning
    {
        [XmlAttribute("first")]
        public Int32 First { get; set; }

        [XmlAttribute("second")]
        public Int32 Second { get; set; }

        [XmlAttribute("amount")]
        public Int32 Amount { get; set; }
    }
}