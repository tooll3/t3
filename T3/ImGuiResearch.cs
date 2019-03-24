using ImGuiNET;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using T3.Core.Operator;

namespace T3
{
    public class Texture2dOperator
    {
        public Slot<Texture2D> Result { get; }

        public Texture2dOperator()
        {
            Result = new Slot<Texture2D>(Update, null);
        }

        public void Update(EvaluationContext context)
        {
            var desc = new Texture2DDescription()
            {
                Width = Size.GetValue(context).Width,
                Height = Size.GetValue(context).Height,
                MipLevels = MipLevels.GetValue(context),
                ArraySize = ArraySize.GetValue(context),
                Format = Format.GetValue(context),
                SampleDescription = new SampleDescription(SampleCount.GetValue(context), SampleQuality.GetValue(context)),
                Usage = Usage.GetValue(context),
                BindFlags = BindFlags.GetValue(context),
                CpuAccessFlags = CpuAccessFlags.GetValue(context),
                OptionFlags = OptionFlags.GetValue(context)
            };

            ResourceManager.Instance().CreateTexture(desc, "OpName", ref _textureResourceId, ref Result.Value);
        }

        private Guid _textureResourceId = Guid.Empty;

        public Size2Slot Size { get; } = new Size2Slot(new Size2(256, 256));
        public InputSlot<int> MipLevels { get; } = new InputSlot<int>(0);
        public InputSlot<int> ArraySize { get; } = new InputSlot<int>(1);
        public InputSlot<Format> Format { get; } = new InputSlot<Format>(SharpDX.DXGI.Format.R8G8B8A8_SNorm);
        public InputSlot<int> SampleCount { get; } = new InputSlot<int>(1);
        public InputSlot<int> SampleQuality { get; } = new InputSlot<int>(0);
        public InputSlot<ResourceUsage> Usage { get; } = new InputSlot<ResourceUsage>(ResourceUsage.Default);
        public InputSlot<BindFlags> BindFlags { get; } = new InputSlot<BindFlags>(SharpDX.Direct3D11.BindFlags.ShaderResource);
        public InputSlot<CpuAccessFlags> CpuAccessFlags { get; } = new InputSlot<CpuAccessFlags>(SharpDX.Direct3D11.CpuAccessFlags.None);
        public InputSlot<ResourceOptionFlags> OptionFlags { get; } = new InputSlot<ResourceOptionFlags>(ResourceOptionFlags.None);
    }


    public class ImGuiResearch
    {
        public class Globals
        {
            public int X;
            public int Y;
        }

        public static int LocalFunc(Globals g) { return g.X * 100 * g.Y; }


        public static void TestScriptDelegateWithVariables()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var script = CSharpScript.Create<int>("X*100*Y", globalsType: typeof(Globals));
            ScriptRunner<int> runner = script.CreateDelegate();
            watch.Stop();
            Console.WriteLine($"compilation took: {(double)watch.ElapsedTicks / Stopwatch.Frequency}s");
            Console.WriteLine($"compilation took: {watch.ElapsedMilliseconds}ms");
            var globals = new Globals();
            int sum = 0;
            watch.Reset();
            watch.Restart();
            for (int i = 0; i < 100; i++)
            {
                globals.X = i;
                globals.Y = i;
                sum += runner(globals).Result;
            }
            watch.Stop();
            Console.WriteLine($"sum: {sum}");
            Console.WriteLine($"calling took: {(double)watch.ElapsedTicks / Stopwatch.Frequency / 100.0}s");
            Console.WriteLine($"calling took: {(double)watch.ElapsedMilliseconds / 100.0}ms");
        }

        public static void TestScriptDelegateWithVariablesComparison()
        {
            var globals = new Globals();
            int sum = 0;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < 100; i++)
            {
                globals.X = i;
                globals.Y = i;
                sum += LocalFunc(globals);
            }
            watch.Stop();
            Console.WriteLine($"sum: {sum}");
            Console.WriteLine($"calling took: {(double)watch.ElapsedTicks / Stopwatch.Frequency / 100.0}s");
            Console.WriteLine($"calling took: {(double)watch.ElapsedMilliseconds / 100.0}ms");
        }



        #region early experiments








        private static Texture2dOperator _textureOperator = new Texture2dOperator();
        public static void DrawParameterView()
        {
            var op = _textureOperator;
            ImGui.Begin("Parameters");

            Type inputSlotType = typeof(IInputSlot);
            Type texOpType = op.GetType();
            var inputInfos = (from property in texOpType.GetProperties()
                              where inputSlotType.IsAssignableFrom(property.PropertyType)
                              select property).ToArray();
            ImGui.Text(texOpType.Name);

            foreach (var inputInfo in inputInfos)
            {
                var input = inputInfo.GetValue(op);
                var inputType = inputInfo.PropertyType;
                FieldInfo valueInfo = inputType.GetField("Value");
                Type valueType = valueInfo.FieldType;

                var typeCode = Type.GetTypeCode(valueType);
                if (typeCode == TypeCode.Int32 && !valueType.IsEnum)
                {
                    var typedInput = (InputSlot<int>)input;
                    ImGui.DragInt(inputInfo.Name, ref typedInput.Value, 1.0f, 0, 1000);
                }
                else if (valueType.IsEnum)
                {
                    var values = Enum.GetValues(valueType);
                    var names = Enum.GetNames(valueType);
                    if (valueType.GetCustomAttributes<FlagsAttribute>().Any())
                    {
                        // show as checkboxes
                        if (ImGui.TreeNode(inputInfo.Name))
                        {
                            bool[] checks = new bool[names.Length];
                            for (int i = 0; i < names.Length; i++)
                            {
                                ImGui.Checkbox(names[i], ref checks[i]);
                            }
                            ImGui.TreePop();
                        }
                    }
                    else
                    {
                        // show as combo
                        int index = (int)valueInfo.GetValue(input);
                        ImGui.Combo(inputInfo.Name, ref index, names, names.Length);
                        valueInfo.SetValue(input, values.GetValue(index));
                    }
                }
                else if (valueType == typeof(Size2))
                {
                    var typedInput = (Size2Slot)input;
                    ImGui.DragInt2(inputInfo.Name, ref typedInput.Value.Width);
                }
                else if (valueType == typeof(Vector3))
                {
                    var typedInput = (InputSlot<Vector3>)input;
                    System.Numerics.Vector3 value = new System.Numerics.Vector3(typedInput.Value.X, typedInput.Value.Y, typedInput.Value.Z);
                    ImGui.DragFloat3(inputInfo.Name, ref value);
                    typedInput.Value = new Vector3(value.X, value.Y, value.Z);
                }

            }

            ImGui.End();
        }


        const int NameEditFieldSize = 30;
        private static bool _openVertexShaderCreation = true;
        private static readonly byte[] _vertexShaderName = new byte[NameEditFieldSize];
        private static readonly byte[] _vertexShaderEntryPoint = new byte[NameEditFieldSize];
        private static string _vertexShaderSourceFile = string.Empty;

        private static bool _openPixelShaderCreation = true;
        private static readonly byte[] _pixelShaderName = new byte[NameEditFieldSize];
        private static readonly byte[] _pixelShaderEntryPoint = new byte[NameEditFieldSize];
        private static string _pixelShaderSourceFile = string.Empty;

        private static bool _openShaderResourceViewCreation = true;
        private static readonly byte[] _shaderResourceViewName = new byte[NameEditFieldSize];
        private static int _shaderResourceViewCurrentIndex = 0;

        //private static 
        private static bool _openTexture2dCreation = true;
        private static readonly byte[] _createTexture2dName = new byte[NameEditFieldSize];
        private static Texture2DDescription _createTexture2dDescription = new Texture2DDescription();
        private static int _createTexture2dUsage = 0;
        private static bool[] _createTexture2dBindFlags = new bool[10];
        private static bool _createTexture2dCpuAccessRead = false, _createTexture2dCpuAccessWrite = false;
        private static bool[] _createTexture2dMiscFlags = new bool[17];

        public static void DrawResourceWindow()
        {
            var resourceManager = ResourceManager.Instance();
            ImGui.Begin("Resources");

            if (ImGui.TreeNode("Vertex Shader"))
            {
                foreach (var shader in resourceManager.VertexShaders)
                {
                    ImGui.Text(shader.Name);
                }

                if (ImGui.Button("New Vertex Shader"))
                {
                    ImGui.OpenPopup("Create Vertex Shader");
                    _openVertexShaderCreation = true;
                }

                if (ImGui.BeginPopupModal("Create Vertex Shader", ref _openVertexShaderCreation, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.InputText("Name ", _vertexShaderName, NameEditFieldSize);
                    ImGui.InputText("Entry Point", _vertexShaderEntryPoint, NameEditFieldSize);
                    ImGui.InputText("Source File", ref _vertexShaderSourceFile, 256);
                    ImGui.SameLine();

                    if (ImGui.Button("Open"))
                    {
                        using (OpenFileDialog openFileDialog = new OpenFileDialog())
                        {
                            openFileDialog.Filter = "hlsl files (*.hlsl)|*.hlsl";
                            if (openFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                _vertexShaderSourceFile = openFileDialog.FileName;
                            }
                        }
                    }

                    if (ImGui.Button("Create", new System.Numerics.Vector2(100, 0)))
                    {
                        var name = System.Text.Encoding.Default.GetString(_vertexShaderName).Trim('\0');
                        var entryPoint = System.Text.Encoding.Default.GetString(_vertexShaderEntryPoint).Trim('\0');
                        resourceManager.CreateVertexShader(_vertexShaderSourceFile, entryPoint, name);
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Cancel", new System.Numerics.Vector2(100, 0)))
                    {
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.EndPopup();
                }

                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Pixel Shader"))
            {
                foreach (var shader in resourceManager.PixelShaders)
                {
                    ImGui.Text(shader.Name);
                }

                if (ImGui.Button("New Pixel Shader"))
                {
                    ImGui.OpenPopup("Create Pixel Shader");
                    _openPixelShaderCreation = true;
                }

                if (ImGui.BeginPopupModal("Create Pixel Shader", ref _openPixelShaderCreation, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.InputText("Name", _pixelShaderName, NameEditFieldSize);
                    ImGui.InputText("Entry Point", _pixelShaderEntryPoint, NameEditFieldSize);
                    ImGui.InputText("Source File", ref _pixelShaderSourceFile, 256);
                    ImGui.SameLine();

                    if (ImGui.Button("Open"))
                    {
                        using (OpenFileDialog openFileDialog = new OpenFileDialog())
                        {
                            openFileDialog.Filter = "hlsl files (*.hlsl)|*.hlsl";
                            if (openFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                _pixelShaderSourceFile = openFileDialog.FileName;
                            }
                        }
                    }

                    if (ImGui.Button("Create", new System.Numerics.Vector2(100, 0)))
                    {
                        var name = System.Text.Encoding.Default.GetString(_pixelShaderName).Trim('\0');
                        var entryPoint = System.Text.Encoding.Default.GetString(_pixelShaderEntryPoint).Trim('\0');
                        resourceManager.CreatePixelShader(_pixelShaderSourceFile, entryPoint, name);
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Cancel", new System.Numerics.Vector2(100, 0)))
                    {
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.EndPopup();
                }

                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Shader Resource Views"))
            {
                foreach (var srvResource in resourceManager.ShaderResourceViews)
                {
                    ImGui.Text(srvResource.Name);
                }

                #region create shader resource view
                if (ImGui.Button("New Shader Resource View"))
                {
                    ImGui.OpenPopup("Create Shader Resource View");
                    _openShaderResourceViewCreation = true;
                }

                if (ImGui.BeginPopupModal("Create Shader Resource View", ref _openShaderResourceViewCreation, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.InputText("Name", _shaderResourceViewName, NameEditFieldSize);

                    var srvCapableTextureResources = new List<TextureResource>();
                    foreach (var textureResource in resourceManager.Textures)
                    {
                        if (textureResource.Texture.Description.BindFlags.HasFlag(BindFlags.ShaderResource))
                        {
                            srvCapableTextureResources.Add(textureResource);
                        }
                    }

                    if (srvCapableTextureResources.Count > 0)
                    {
                        var names = new string[srvCapableTextureResources.Count];
                        for (int i = 0; i < names.Length; i++)
                            names[i] = srvCapableTextureResources[i].Texture.DebugName;

                        ImGui.Combo("Texture", ref _shaderResourceViewCurrentIndex, names, names.Length);

                        if (ImGui.Button("Create", new System.Numerics.Vector2(100, 0)))
                        {
                            var name = System.Text.Encoding.Default.GetString(_shaderResourceViewName).Trim('\0');
                            Guid textureId = srvCapableTextureResources[_shaderResourceViewCurrentIndex].Id;
                            resourceManager.CreateShaderResourceView(textureId, name);
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.SameLine();
                    }
                    if (ImGui.Button("Cancel", new System.Numerics.Vector2(100, 0)))
                    {
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.EndPopup();
                }
                #endregion

                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Textures"))
            {
                foreach (var textureResource in resourceManager.Textures)
                {
                    ImGui.Text(textureResource.Name);
                }

                if (ImGui.Button("Load Texture"))
                {
                    using (OpenFileDialog openFileDialog = new OpenFileDialog())
                    {
                        openFileDialog.Filter = "image files (*.jpg)|*.jpg";
                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            resourceManager.CreateTextureFromFile(openFileDialog.FileName);
                        }
                    }
                }

                ImGui.SameLine();

                if (ImGui.Button("Create Texture"))
                {
                    ImGui.OpenPopup("Create Texture 2d");
                }

                if (ImGui.BeginPopupModal("Create Texture 2d", ref _openTexture2dCreation, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.InputText("Name", _createTexture2dName, NameEditFieldSize);
                    ImGui.DragInt2("Width/Height", ref _createTexture2dDescription.Width);
                    ImGui.DragInt("Mip Levels", ref _createTexture2dDescription.MipLevels, 1.0f, 0, (int)Math.Max(Math.Log(_createTexture2dDescription.Width) / Math.Log(2.0), Math.Log(_createTexture2dDescription.Height) / Math.Log(2.0)));
                    ImGui.DragInt("Array Size", ref _createTexture2dDescription.ArraySize, 1.0f, 1, 2048); //D3D11_REQ_TEXTURE2D_ARRAY_AXIS_DIMENSION
                    //ImGui::Combo("Format", (int*)&textureDesc.Format, formats, IM_ARRAYSIZE(formats));
                    ImGui.DragInt("Sample Count", ref _createTexture2dDescription.SampleDescription.Count, 1.0f, 1, 16);
                    ImGui.DragInt("Sample Quality", ref _createTexture2dDescription.SampleDescription.Quality, 1.0f, 0, 16);// use CheckMultisampleQualityLevels
                    ImGui.Combo("Usage", ref _createTexture2dUsage, "Default\0Immutable\0Dynamic\0Staging\0\0");

                    if (_createTexture2dDescription.BindFlags.GetType().IsEnum)
                    {
                        var bindFlagsType = _createTexture2dDescription.BindFlags.GetType();
                        var values = Enum.GetValues(bindFlagsType).Cast<BindFlags>();
                        var names = Enum.GetNames(bindFlagsType);

                        if (ImGui.TreeNode(nameof(_createTexture2dDescription.BindFlags)))
                        {
                            if (bindFlagsType.GetCustomAttributes<FlagsAttribute>().Any())
                            {
                                // show as checkboxes
                                bool[] checks = new bool[names.Length];
                                for (int i = 0; i < names.Length; i++)
                                {
                                    ImGui.Checkbox(names[i], ref checks[i]);
                                }
                            }
                            else
                            {
                                // show as combo
                            }
                            ImGui.TreePop();
                        }
                    }

                    if (_createTexture2dDescription.Format.GetType().IsEnum)
                    {
                        var formatFlagsType = _createTexture2dDescription.Format.GetType();
                        var values = Enum.GetValues(formatFlagsType).Cast<Format>();
                        var names = Enum.GetNames(formatFlagsType);

                        if (formatFlagsType.GetCustomAttributes<FlagsAttribute>().Any())
                        {
                            // show as checkboxes
                            if (ImGui.TreeNode(nameof(_createTexture2dDescription.Format)))
                            {
                                bool[] checks = new bool[names.Length];
                                for (int i = 0; i < names.Length; i++)
                                {
                                    ImGui.Checkbox(names[i], ref checks[i]);
                                }
                            }
                            ImGui.TreePop();
                        }
                        else
                        {
                            // show as combo
                            int _formatIndex = 0;
                            ImGui.Combo(nameof(_createTexture2dDescription.Format), ref _formatIndex, names, names.Length);
                        }
                    }


                    if (ImGui.TreeNode("Bind Flags"))
                    {
                        ImGui.Checkbox("Vertex Buffer", ref _createTexture2dBindFlags[0]);
                        ImGui.Checkbox("Index Buffer", ref _createTexture2dBindFlags[1]);
                        ImGui.Checkbox("Constant Buffer", ref _createTexture2dBindFlags[2]);
                        ImGui.Checkbox("Shader Resource", ref _createTexture2dBindFlags[3]);
                        ImGui.Checkbox("Stream Output", ref _createTexture2dBindFlags[4]);
                        ImGui.Checkbox("Render Target", ref _createTexture2dBindFlags[5]);
                        ImGui.Checkbox("Depth Stencil", ref _createTexture2dBindFlags[6]);
                        ImGui.Checkbox("Unordered Access", ref _createTexture2dBindFlags[7]);
                        ImGui.Checkbox("Decoder", ref _createTexture2dBindFlags[8]);
                        ImGui.Checkbox("Video Encoder", ref _createTexture2dBindFlags[9]);

                        ImGui.TreePop();
                    }
                    if (ImGui.TreeNode("CPU Access"))
                    {
                        ImGui.Checkbox("Write", ref _createTexture2dCpuAccessWrite);
                        ImGui.Checkbox("Read", ref _createTexture2dCpuAccessRead);
                        ImGui.TreePop();
                    }
                    if (ImGui.TreeNode("Misc Flags"))
                    {
                        ImGui.Checkbox("Generate Mips", ref _createTexture2dMiscFlags[0]);
                        ImGui.Checkbox("Shared", ref _createTexture2dMiscFlags[1]);
                        ImGui.Checkbox("Texture Cube", ref _createTexture2dMiscFlags[2]);
                        ImGui.Checkbox("Draw Indirect", ref _createTexture2dMiscFlags[3]);
                        ImGui.Checkbox("Buffer Allow Raw View", ref _createTexture2dMiscFlags[4]);
                        ImGui.Checkbox("Buffer Structured", ref _createTexture2dMiscFlags[5]);
                        ImGui.Checkbox("Resource Clamp", ref _createTexture2dMiscFlags[6]);
                        ImGui.Checkbox("Shared Keyed Mutex", ref _createTexture2dMiscFlags[7]);
                        ImGui.Checkbox("GDI Compatible", ref _createTexture2dMiscFlags[8]);
                        ImGui.Checkbox("Shared NT Handle", ref _createTexture2dMiscFlags[9]);
                        ImGui.Checkbox("Restricted Content", ref _createTexture2dMiscFlags[10]);
                        ImGui.Checkbox("Restricted Shared Resource", ref _createTexture2dMiscFlags[11]);
                        ImGui.Checkbox("Restricted Shared Resource Driver", ref _createTexture2dMiscFlags[12]);
                        ImGui.Checkbox("Guarded", ref _createTexture2dMiscFlags[13]);
                        ImGui.Checkbox("Tile Pool", ref _createTexture2dMiscFlags[14]);
                        ImGui.Checkbox("Tiled", ref _createTexture2dMiscFlags[15]);
                        ImGui.Checkbox("HW Protected", ref _createTexture2dMiscFlags[16]);

                        ImGui.TreePop();
                    }

                    if (ImGui.Button("Create", new System.Numerics.Vector2(100, 0)))
                    {
                        var name = System.Text.Encoding.Default.GetString(_createTexture2dName).Trim('\0');
                        for (int i = 0; i < _createTexture2dBindFlags.Length; i++)
                            _createTexture2dDescription.BindFlags |= (BindFlags)((_createTexture2dBindFlags[i] ? 1 : 0) << i);
                        _createTexture2dDescription.CpuAccessFlags = (_createTexture2dCpuAccessWrite ? CpuAccessFlags.Write : CpuAccessFlags.None)
                                                                     | (_createTexture2dCpuAccessRead ? CpuAccessFlags.Read : CpuAccessFlags.None);
                        for (int i = 0; i < _createTexture2dMiscFlags.Length; i++)
                            _createTexture2dDescription.OptionFlags |= (ResourceOptionFlags)((_createTexture2dMiscFlags[i] ? 1 : 0) << i);

                        Texture2D texture = null;
                        Guid id = Guid.Empty;
                        resourceManager.CreateTexture(_createTexture2dDescription, name, ref id, ref texture);
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Cancel", new System.Numerics.Vector2(100, 0)))
                    {
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.EndPopup();
                }
            }

            ImGui.End();
        }
        #endregion
    }
}
