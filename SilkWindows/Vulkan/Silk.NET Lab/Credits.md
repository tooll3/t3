This work is heavily based on the work of [Silk.NET](https://github.com/dotnet/Silk.NET), and of course uses their libraries extensively.

The following were copied directly from their work with modifications made to fit our pipeline:

- [imgui-background.frag.glsl](./Shaders/imgui-background.frag.glsl)
- [imgui-background.vert.glsl](./Shaders/imgui-background.vert.glsl)
- [triangle.frag.glsl](./Shaders/triangle.frag.glsl)
- [triangle.vert.glsl](./Shaders/triangle.vert.glsl)
- [ImGuiVulkanWindowImpl](./ImGuiVulkanWindowImpl.cs) - modified to fit our imgui interface and datatypes, added runtime shader compilation
- [ImGuiControllerVk.*](./ImGuiControllerVk.cs) - modified to fit our imgui interface and datatypes, added runtime shader compilation

Their work is MIT licensed under the .NET Foundation. 
It is highly encouraged that you donate or otherwise contribute to their work, as it's invaluable for the C#/.NET community