// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using ImguiWindows;
using Silk.NET.Input;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.ImGui;
using Silk.NET.Windowing;
using SilkWindows;

namespace ImGuiVulkan;

public class VulkanImGuiImpl(ImGuiVulkanWindowImpl vulkanWindow, Vk vk, IWindow window, IInputContext inputContext)
    : IImguiImplementation
{
    public string Title { get; } = window.Title;
    
    public IntPtr InitializeControllerContext(Action? onConfigureIO)
    {
        // todo - font rendering is busted?
        _imGuiController = new ImGuiController
        (
            vk,
            window,
            inputContext,
            vulkanWindow.PhysicalDevice,
            vulkanWindow.GraphicsFamilyIndex,
            vulkanWindow.SwapChainLength,
            vulkanWindow.SwapChainImageFormat,
            null
        );
       
        onConfigureIO?.Invoke();
        return _imGuiController.Context;
    }
    
    public void StartImguiFrame(float deltaSeconds)
    {
        // Make sure ImGui is up-to-date before rendering
        _imGuiController.Update(deltaSeconds);
    }
    
    public void EndImguiFrame()
    {
        vulkanWindow.GetFrameInfo(out var commandBuffer, out var frameBuffer, out var swapChainExtent);
        _imGuiController.Render(commandBuffer, frameBuffer, swapChainExtent);
    }
    
    public void Dispose()
    {
        vulkanWindow.WaitForIdle();
        _imGuiController.Dispose();
    }
    
    private ImGuiController _imGuiController;
}
