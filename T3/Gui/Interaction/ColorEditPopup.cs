using System;
using System.Numerics;
using ImGuiNET;
using T3.App;
using T3.Core;
using T3.Core.Logging;
using T3.Gui.InputUi;
using UiHelpers;

namespace T3.Gui.Interaction
{
    public static class ColorEditPopup
    {
        public static bool DrawPopup(ref Vector4 color, Vector4 previousColor)
        {
            var edited = false;
            var cColor = new Color(color);
            const float saturationWarp = 1.5f; 
            ImGui.SetNextWindowSize(new Vector2(270, 290));
            if (ImGui.BeginPopup("##colorEdit"))
            {
                var drawList = ImGui.GetForegroundDrawList();

                ImGui.ColorConvertRGBtoHSV(color.X, color.Y, color.Z, out var hNormalized, out var linearSaturation, out var v);

                var wheelRadius = 209f;
                var windowPos = ImGui.GetCursorScreenPos() + new Vector2(10,10);
                var size = new Vector2(wheelRadius, wheelRadius);
                var clampedV = v.Clamp(0, 1);
                
                const float colorEdgeWidth = 3;
                drawList.AddImage((IntPtr)SharedResources.ColorPickerImageSrv, windowPos- Vector2.One * colorEdgeWidth, windowPos + size + Vector2.One * colorEdgeWidth);
                drawList.AddImage((IntPtr)SharedResources.ColorPickerImageSrv, windowPos, windowPos + size, Vector2.Zero, Vector2.One, new Color(clampedV, clampedV, clampedV));
                
                var hueAngle = (hNormalized + 0.25f) * 2 * MathF.PI;
                var warpedSaturation = MathF.Pow(linearSaturation, 1 / saturationWarp);
                var pickedColorPos = new Vector2(MathF.Sin(hueAngle), MathF.Cos(hueAngle)) * size / 2 * warpedSaturation + size / 2;

                {
                    //var previousC = new Color(previousColor);
                    ImGui.ColorConvertRGBtoHSV(previousColor.X, previousColor.Y, previousColor.Z, out var prevHueNormalized, out var prevLinearSaturation, out var pv);
                    var pHueAngle = (prevHueNormalized + 0.25f) * 2 * MathF.PI;
                    var pWarpedSaturation = MathF.Pow(prevLinearSaturation, 1 / saturationWarp);
                    var pPickedColorPos = new Vector2(MathF.Sin(pHueAngle), MathF.Cos(pHueAngle)) * size / 2 * pWarpedSaturation + size / 2;
                    drawList.AddCircle(windowPos + pPickedColorPos, 2, Color.Black.Fade(0.3f));
                    drawList.AddCircle(windowPos + pPickedColorPos, 1, Color.White.Fade(0.5f));
                }
                
                
                drawList.AddCircle(windowPos + pickedColorPos, 5, Color.Black);
                drawList.AddCircle(windowPos + pickedColorPos, 4, Color.White);
                drawList.AddCircle(Vector2.Zero, 100, Color.Green);
                ImGui.InvisibleButton("colorwheel", size);
                if (ImGui.IsItemActive())
                {
                    var localPosition = ImGui.GetMousePos() - windowPos - size / 2;
                    edited = true;

                    hNormalized = (MathF.Atan2(localPosition.X, localPosition.Y) / (2 * MathF.PI) - 0.25f);
                    if (hNormalized < 0)
                    {
                        hNormalized = 1 + hNormalized;
                    }

                    var saturation = MathUtils.Clamp(localPosition.Length() / size.X * 2f, 0, 1);
                    linearSaturation = MathF.Pow(saturation, saturationWarp);

                    cColor = Color.FromHSV(hNormalized, linearSaturation, v, cColor.A);
                }


                // Draw value slider 
                {
                    var barHeight = wheelRadius;
                    const float barWidth =10;
                    var pMin = windowPos + new Vector2(size.X + 10, 0);
                    var visibleBarSize = new Vector2(barWidth, barHeight);
                    var pMax = pMin + visibleBarSize;
                    drawList.AddRectFilled(pMin - Vector2.One, pMax + Vector2.One, Color.Black);
                    
                    var brightColor = cColor;
                    brightColor.V = 1;
                    brightColor.A = 1; 
                    
                    var transparentColor = color;
                    transparentColor.W = 0;
                    drawList.AddRectFilledMultiColor(pMin, pMax,
                                                     ImGui.ColorConvertFloat4ToU32(brightColor),
                                                     ImGui.ColorConvertFloat4ToU32(brightColor),
                                                     ImGui.ColorConvertFloat4ToU32(transparentColor),
                                                     ImGui.ColorConvertFloat4ToU32(transparentColor));

                    var handlePos = new Vector2(0, barHeight * (1- cColor.V)) + pMin;
                    
                    drawList.AddRectFilled(handlePos - Vector2.One, handlePos + new Vector2(barWidth + 2, 2), Color.Black);
                    drawList.AddRectFilled(handlePos, handlePos + new Vector2(barWidth + 2, 1), Color.White);       
                    ImGui.SetCursorScreenPos(pMin - new Vector2(10,0));
                    ImGui.InvisibleButton("intensitySlider", new Vector2(visibleBarSize.X * 4, visibleBarSize.Y ));
                    if (ImGui.IsItemActive())
                    {
                        var clampUpperValue = ImGui.GetIO().KeyCtrl ? 100 : 1;
                        var normalizedValue = (1- (ImGui.GetMousePos() - pMin).Y / barHeight).Clamp(0,clampUpperValue);
                        if (normalizedValue > 1)
                        {
                            normalizedValue = MathF.Pow(normalizedValue, 3);
                        }
                        cColor.V = normalizedValue;
                        
                        edited = true;
                    }
                }
                
                // Draw alpha slider 
                {
                    var barHeight = 10;
                    var barWidth = wheelRadius;
                    
                    var pMin = windowPos + new Vector2(0, size.X + 10);
                    var barSize = new Vector2(barWidth, barHeight);
                    var pMax = pMin + barSize;
                    
                    var area = new ImRect(pMin, pMax);
                    drawList.AddRectFilled(pMin - Vector2.One, pMax + Vector2.One, new Color(0.1f, 0.1f, 0.1f));
                    CustomComponents.FillWithStripes(drawList, area);
                    
                    var opaqueColor = cColor;
                    opaqueColor.A = 1; 
                    
                    var transparentColor = color;
                    transparentColor.W = 0;
                    drawList.AddRectFilledMultiColor(pMin, pMax,
                                                     ImGui.ColorConvertFloat4ToU32(transparentColor),
                                                     ImGui.ColorConvertFloat4ToU32(opaqueColor),
                                                     ImGui.ColorConvertFloat4ToU32(opaqueColor),
                                                     ImGui.ColorConvertFloat4ToU32(transparentColor));

                    var handlePos = new Vector2(barWidth * cColor.A,0) + pMin;
                    
                    drawList.AddRectFilled(handlePos - Vector2.One, 
                                           handlePos + new Vector2(2, barHeight + 2), Color.Black);
                    drawList.AddRectFilled(handlePos, 
                                           handlePos + new Vector2(1, barHeight + 2), Color.White);
                    
                    ImGui.SetCursorScreenPos(pMin - new Vector2(0,10));
                    
                    ImGui.InvisibleButton("alphaSlider", new Vector2(barSize.X, barSize.Y  * 3));
                    if (ImGui.IsItemActive())
                    {
                        cColor.A = ((ImGui.GetMousePos() - pMin).X / barWidth).Clamp(0,1);
                        edited = true;
                    }
                }                

                // Draw HSV input values
                {
                    var inputSize = new Vector2(60,
                                                ImGui.GetFrameHeight());
                    var hueDegrees = hNormalized * 360f;

                    ImGui.PushID("h");
                    if (SingleValueEdit.Draw(ref hueDegrees, inputSize, 0, 360, false, 1, "{0:0.0}") is InputEditStateFlags.Modified)
                    {
                        if (hueDegrees < 360)
                        {
                            hueDegrees += 360;
                        }
                        else if (hueDegrees > 360)
                        {
                            hueDegrees -= 360;
                        }

                        cColor.Hue = hueDegrees / 360;
                        edited = true;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.PushID("s");
                    if (SingleValueEdit.Draw(ref linearSaturation, inputSize, 0, 1, true,
                                             scale: 0.002f,
                                             format: "{0:0.00}") is InputEditStateFlags.Modified)
                    {
                        cColor.Saturation = linearSaturation.Clamp(0, 1);
                        edited = true;
                    }
                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.PushID("v");
                    if (SingleValueEdit.Draw(ref v, inputSize, 0, 20, true, 0.020f, "{0:0.00}") is InputEditStateFlags.Modified)
                    {
                        cColor.V = v.Clamp(0, 10);
                        edited = true;
                    }
                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.Dummy(Vector2.One * 20);
                    
                    ImGui.SameLine();
                    ImGui.PushID("a");
                    var a = cColor.A;
                    if (SingleValueEdit.Draw(ref a, inputSize, 0, 20, true, 0.020f, "{0:0.00}") is InputEditStateFlags.Modified)
                    {
                        cColor.A = a.Clamp(0, 1);
                        edited = true;
                    }
                    ImGui.PopID();
                }

                ImGui.EndPopup();
            }

            color = cColor.Rgba;
            return edited;
        }
    }
}