// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using ImguiWindows;
using Silk.NET.Input;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using SilkWindows.Vulkan;

namespace ImGuiVulkan;

internal sealed class VulkanImGuiImpl(SilkWindows.Vulkan.Silk.NET_Lab.ImGuiVulkanWindowImpl vulkanWindow, KhrDynamicRendering rendering, Silk.NET.Vulkan.Vk vk, IWindow window, IInputContext inputContext)
    : IImguiImplementation
{
    public string Title { get; } = window.Title;
    
    public IntPtr InitializeControllerContext(Action? onConfigureIO)
    {
        // todo - font rendering is busted?
        _imGuiControllerVk = new ImGuiControllerVk
        (
            vk,
            rendering,
            window,
            inputContext,
            vulkanWindow.PhysicalDevice,
            vulkanWindow.GraphicsFamilyIndex,
            vulkanWindow.SwapChainLength,
            vulkanWindow.SwapChainImageFormat,
            null,
            onConfigure: onConfigureIO
        );
       
        return _imGuiControllerVk.Context;
    }
    
    public void StartImguiFrame(float deltaSeconds)
    {
        // Make sure ImGui is up-to-date before rendering
        _imGuiControllerVk.Update(deltaSeconds);
    }
    
    public void EndImguiFrame()
    {
        vulkanWindow.GetFrameInfo(out var commandBuffer, out var swapChainExtent);
        _imGuiControllerVk.Render(commandBuffer, swapChainExtent);
    }
    
    public void Dispose()
    {
        vulkanWindow.WaitForIdle();
        _imGuiControllerVk.Dispose();
    }
    
    private ImGuiControllerVk _imGuiControllerVk = default!;
}
