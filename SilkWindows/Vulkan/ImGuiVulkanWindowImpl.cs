// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using ImguiWindows;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Windowing;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace ImGuiVulkan;

public partial class ImGuiVulkanWindowImpl : IWindowImplementation
{
    public ImGuiVulkanWindowImpl(WindowOptions options)
    {
        options.IsEventDriven = EventBasedRendering;
        options.API = GraphicsAPI.DefaultVulkan;
        WindowOptions = options;
    }
    
    public WindowOptions WindowOptions { get; }
    public Color DefaultClearColor { get; } = Color.Black;
    
    public NativeAPI InitializeGraphicsAndInputContexts(IWindow window, out IInputContext inputContext)
    {
        _window = window;
        inputContext = _window.CreateInput();
        _inputContext = inputContext;
        
        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateSwapChain();
        CreateImageViews();
        CreateRenderPass();
        CreateGraphicsPipeline();
        CreateFramebuffers();
        CreateCommandPool();
        CreateCommandBuffers();
        CreateSyncObjects();
        return _vk;
    }
    
    public unsafe bool Render(in Color clearColor, double deltaTime)
    {
        // Wait for any fences
        var fence = _inFlightFences[_currentFrame];
        _vk.WaitForFences(_device, 1, in fence, Vk.True, ulong.MaxValue);
        
        // Manage swapchain
        uint imageIndex;
        _beginFrameResult = _vkSwapchain.AcquireNextImage
            (_device, _swapchain, ulong.MaxValue, _imageAvailableSemaphores[_currentFrame], default, &imageIndex);
        
        if (_beginFrameResult == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapChain();
            return false;
        }
        
        if(!IsResultAcceptable(_beginFrameResult))
        {
            throw new Exception("failed to acquire swap chain image!");
            return false;
        }
        
        _currentImageIndex = imageIndex;
        
        if (_imagesInFlight[imageIndex].Handle != 0)
        {
            _vk.WaitForFences(_device, 1, in _imagesInFlight[imageIndex], Vk.True, ulong.MaxValue);
        }
        
        _imagesInFlight[imageIndex] = _inFlightFences[_currentFrame];
        
        // Render
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.SimultaneousUseBit //
        };
        
        if (_vk.BeginCommandBuffer(_commandBuffers[imageIndex], &beginInfo) != Result.Success)
        {
            throw new Exception("failed to begin recording command buffer!");
        }
        
        // Render triangle
        var renderPassInfo = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = _renderPass,
            Framebuffer = _swapchainFramebuffers[imageIndex],
            RenderArea = { Offset = new Offset2D { X = 0, Y = 0 }, Extent = _swapchainExtent }
        };
        
        var clearColorVal = new ClearValue
        {
            Color = new ClearColorValue
            {
                // ARGB
                Float32_0 = clearColor.A / 255f,
                Float32_1 = clearColor.R / 255f,
                Float32_2 = clearColor.G / 255f,
                Float32_3 = clearColor.B / 255f
            }
        };
        
        renderPassInfo.ClearValueCount = 1;
        renderPassInfo.PClearValues = &clearColorVal;
        
        _vk.CmdBeginRenderPass(_commandBuffers[imageIndex], &renderPassInfo, SubpassContents.Inline);
        
        _vk.CmdBindPipeline(_commandBuffers[imageIndex], PipelineBindPoint.Graphics, _graphicsPipeline);
        
        _vk.CmdDraw(_commandBuffers[imageIndex], 3, 1, 0, 0);
        
        _vk.CmdEndRenderPass(_commandBuffers[imageIndex]);
        return true;
    }
    
    private static bool IsResultAcceptable(Result result) => result is Result.Success or Result.SuboptimalKhr;
    
    public unsafe void EndRender()
    {
        var imageIndex = _currentImageIndex;
        
        if (_vk.EndCommandBuffer(_commandBuffers[imageIndex]) != Result.Success)
        {
            throw new Exception("failed to record command buffer!");
        }
        
        var submitInfo = new SubmitInfo { SType = StructureType.SubmitInfo };
        
        Semaphore[] waitSemaphores = [_imageAvailableSemaphores[_currentFrame]];
        PipelineStageFlags[] waitStages = [PipelineStageFlags.ColorAttachmentOutputBit];
        submitInfo.WaitSemaphoreCount = 1;
        var signalSemaphore = _renderFinishedSemaphores[_currentFrame];
        var fence = _inFlightFences[_currentFrame];
        
        fixed (Semaphore* waitSemaphoresPtr = waitSemaphores)
        {
            fixed (PipelineStageFlags* waitStagesPtr = waitStages)
            {
                submitInfo.PWaitSemaphores = waitSemaphoresPtr;
                submitInfo.PWaitDstStageMask = waitStagesPtr;
                
                submitInfo.CommandBufferCount = 1;
                var buffer = _commandBuffers[imageIndex];
                submitInfo.PCommandBuffers = &buffer;
                
                submitInfo.SignalSemaphoreCount = 1;
                submitInfo.PSignalSemaphores = &signalSemaphore;
                
                _vk.ResetFences(_device, 1, &fence);
                
                if (_vk.QueueSubmit
                        (_graphicsQueue, 1, &submitInfo, _inFlightFences[_currentFrame]) != Result.Success)
                {
                    throw new Exception("failed to submit draw command buffer!");
                }
            }
        }
        
        Result result;
        
        fixed (SwapchainKHR* swapchain = &_swapchain)
        {
            var presentInfo = new PresentInfoKHR
            {
                SType = StructureType.PresentInfoKhr,
                WaitSemaphoreCount = 1,
                PWaitSemaphores = &signalSemaphore,
                SwapchainCount = 1,
                PSwapchains = swapchain,
                PImageIndices = &imageIndex
            };
            
            result = _vkSwapchain.QueuePresent(_presentQueue, &presentInfo);
        }
        
        if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || _framebufferResized)
        {
            _framebufferResized = false;
            RecreateSwapChain();
        }
        else if (result != Result.Success)
        {
            throw new Exception("failed to present swap chain image!");
        }
        
        _currentFrame = (_currentFrame + 1) % MaxFramesInFlight;
    }
    
    public unsafe void Dispose()
    {
        WaitForIdle();
        //_inputContext.Dispose();
        
        CleanupSwapchain();
        
        for (var i = 0; i < MaxFramesInFlight; i++)
        {
            _vk.DestroySemaphore(_device, _renderFinishedSemaphores[i], null);
            _vk.DestroySemaphore(_device, _imageAvailableSemaphores[i], null);
            _vk.DestroyFence(_device, _inFlightFences[i], null);
        }
        
        _vk.DestroyCommandPool(_device, _commandPool, null);
        
        _vk.DestroyDevice(_device, null);
        
        if (_validationLayersEnabled)
        {
            _debugUtils.DestroyDebugUtilsMessenger(_instance, _debugMessenger, null);
        }
        
        _vkSurface.DestroySurface(_instance, _surface, null);
        _vk.DestroyInstance(_instance, null);
    }
    
    public IImguiImplementation GetImguiImplementation()
    {
        return new VulkanImGuiImpl(this, _vk, _window, _inputContext);
    }
    
    public void OnWindowResize(Vector2D<int> size)
    {
        _framebufferResized = true; // seems like we're doing this twice - once here and once in EndRender()
        // this may be necessary if we're not continuously drawing the window, but we are... 
        RecreateSwapChain();
        _window.DoRender();
    }
    
    
    private Result _beginFrameResult;
    private uint _currentImageIndex;
    private IWindow _window;
    
    private IInputContext _inputContext;
    
    private Instance _instance;
    private DebugUtilsMessengerEXT _debugMessenger;
    private SurfaceKHR _surface;
    
    private PhysicalDeviceFeatures _physicalDeviceFeatures;
    
    internal PhysicalDevice PhysicalDevice => _physicalDevice;
    private PhysicalDevice _physicalDevice;
    private Device _device;
    
    internal uint GraphicsFamilyIndex => _graphicsFamilyIndex;
    private uint _graphicsFamilyIndex;
    private Queue _graphicsQueue;
    private Queue _presentQueue;
    
    private SwapchainKHR _swapchain;
    internal int SwapChainLength => _swapchainImages.Length;
    private Image[] _swapchainImages;
    public Format SwapChainImageFormat => _swapchainImageFormat;
    private Format _swapchainImageFormat;
    private Extent2D _swapchainExtent;
    private ImageView[] _swapchainImageViews;
    private Framebuffer[] _swapchainFramebuffers;
    
    private RenderPass _renderPass;
    private PipelineLayout _pipelineLayout;
    private Pipeline _graphicsPipeline;
    
    private CommandPool _commandPool;
    private CommandBuffer[] _commandBuffers;
    
    private Semaphore[] _imageAvailableSemaphores;
    private Semaphore[] _renderFinishedSemaphores;
    private Fence[] _inFlightFences;
    private Fence[] _imagesInFlight;
    private uint _currentFrame;
    
    private bool _framebufferResized = false;
    
    private Vk _vk;
    private KhrSurface _vkSurface;
    private KhrSwapchain _vkSwapchain;
    private ExtDebugUtils _debugUtils;
    private string[] _validationLayers = ["VK_LAYER_KHRONOS_validation"];
    private readonly List<string> _instanceExtensions = [ExtDebugUtils.ExtensionName];
    private readonly List<string> _deviceExtensions = [KhrSwapchain.ExtensionName];
    
    // validation
    public enum ValidationModes
    {
        Requested,
        None
    }
    
    private bool _validationLayersEnabled;
    private const ValidationModes ValidationMode = ValidationModes.Requested;
    
    private const int MaxFramesInFlight = 8;
    private const bool EventBasedRendering = false;
    
    internal void GetFrameInfo(out CommandBuffer commandBuffer, out Framebuffer frameBuffer, out Extent2D swapchainExtent)
    {
        commandBuffer = _commandBuffers[_currentImageIndex];
        frameBuffer = _swapchainFramebuffers[_currentImageIndex];
        swapchainExtent = _swapchainExtent;
    }
    
    internal void WaitForIdle()
    {
        _vk.WaitForFences(_device, 1, in _inFlightFences[_currentFrame], Vk.True, ulong.MaxValue);
        _vk.QueueWaitIdle(_graphicsQueue);
        _vk.QueueWaitIdle(_presentQueue);
        _vk.DeviceWaitIdle(_device);
    }
}
