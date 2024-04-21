// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Result = Silk.NET.Vulkan.Result;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace ImGuiVulkan;

public partial class ImGuiVulkanWindowImpl
{
    private unsafe void CleanupSwapchain()
    {
        foreach (var framebuffer in _swapchainFramebuffers)
        {
            _vk.DestroyFramebuffer(_device, framebuffer, null);
        }
        
        fixed (CommandBuffer* buffers = _commandBuffers)
        {
            _vk.FreeCommandBuffers(_device, _commandPool, (uint) _commandBuffers.Length, buffers);
        }
        
        _vk.DestroyPipeline(_device, _graphicsPipeline, null);
        _vk.DestroyPipelineLayout(_device, _pipelineLayout, null);
        _vk.DestroyRenderPass(_device, _renderPass, null);
        
        foreach (var imageView in _swapchainImageViews)
        {
            _vk.DestroyImageView(_device, imageView, null);
        }
        
        _vkSwapchain.DestroySwapchain(_device, _swapchain, null);
    }
    
    private unsafe void CreateInstance()
    {
        _vk = Silk.NET.Vulkan.Vk.GetApi();
        
        var isMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        
        if (isMacOs)
        {
            _instanceExtensions.Add("VK_KHR_portability_enumeration");
            _deviceExtensions.Add("VK_KHR_portability_subset");
        }
        
        CheckValidationLayerSupport();
        
        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*) Marshal.StringToHGlobalAnsi(_window.Title),
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = (byte*) Marshal.StringToHGlobalAnsi("No Engine"),
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Silk.NET.Vulkan.Vk.Version13
        };
        
        var createInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };
        
        if (isMacOs)
        {
            createInfo.Flags = InstanceCreateFlags.EnumeratePortabilityBitKhr;
        }
        
        var extensions = _window.VkSurface!.GetRequiredExtensions(out var extCount);
        // TODO Review that this count doesn't realistically exceed 1k (recommended max for stackalloc)
        // Should probably be allocated on heap anyway as this isn't super performance critical.
        var newExtensions = stackalloc byte*[(int) (extCount + _instanceExtensions.Count)];
        for (var i = 0; i < extCount; i++)
        {
            newExtensions[i] = extensions[i];
        }
        
        for (var i = 0; i < _instanceExtensions.Count; i++)
        {
            newExtensions[extCount + i] = (byte*) SilkMarshal.StringToPtr(_instanceExtensions[i]);
        }
        
        extCount += (uint) _instanceExtensions.Count;
        createInfo.EnabledExtensionCount = extCount;
        createInfo.PpEnabledExtensionNames = newExtensions;
        
        if (_validationLayersEnabled)
        {
            createInfo.EnabledLayerCount = (uint) _validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**) SilkMarshal.StringArrayToPtr(_validationLayers);
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
            createInfo.PNext = null;
        }
        
        fixed (Instance* instance = &_instance)
        {
            if (_vk.CreateInstance(&createInfo, null, instance) != Result.Success)
            {
                throw new Exception("Failed to create instance!");
            }
        }
        
        _vk.CurrentInstance = _instance;
        
        if (!_vk.TryGetInstanceExtension(_instance, out _vkSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }
        
        Marshal.FreeHGlobal((nint) appInfo.PApplicationName);
        Marshal.FreeHGlobal((nint) appInfo.PEngineName);
        
        if (_validationLayersEnabled)
        {
            SilkMarshal.Free((nint) createInfo.PpEnabledLayerNames);
        }
    }
    
    private unsafe void CreateSurface()
    {
        _surface = _window.VkSurface!.Create<AllocationCallbacks>(_instance.ToHandle(), null).ToSurface();
    }
    
    private unsafe void PickPhysicalDevice()
    {
        var devices = _vk.GetPhysicalDevices(_instance);
        
        if (!devices.Any())
        {
            throw new NotSupportedException("Failed to find GPUs with Vulkan support.");
        }
        
        _physicalDevice = devices.FirstOrDefault
        (
            device =>
            {
                var indices = FindQueueFamilies(device);
                
                var extensionsSupported = CheckDeviceExtensionSupport(device);
                
                var swapChainAdequate = false;
                if (extensionsSupported)
                {
                    var swapChainSupport = QuerySwapChainSupport(device);
                    swapChainAdequate = swapChainSupport.Formats.Length != 0 &&
                                        swapChainSupport.PresentModes.Length != 0;
                }
                
                return indices.IsComplete() && extensionsSupported && swapChainAdequate;
            }
        );
        
        if (_physicalDevice.Handle == 0)
            throw new Exception("No suitable device.");
        
        _vk.GetPhysicalDeviceFeatures(_physicalDevice, out _physicalDeviceFeatures);
    }
    
    // Caching the returned values breaks the ability for resizing the window
    private unsafe SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice device)
    {
        var details = new SwapChainSupportDetails();
        _vkSurface.GetPhysicalDeviceSurfaceCapabilities(device, _surface, out var surfaceCapabilities);
        details.Capabilities = surfaceCapabilities;
        
        var formatCount = 0u;
        _vkSurface.GetPhysicalDeviceSurfaceFormats(device, _surface, &formatCount, null);
        
        if (formatCount != 0)
        {
            details.Formats = new SurfaceFormatKHR[formatCount];
            
            using var mem = GlobalMemory.Allocate((int) formatCount * sizeof(SurfaceFormatKHR));
            var formats = (SurfaceFormatKHR*) Unsafe.AsPointer(ref mem.GetPinnableReference());
            
            _vkSurface.GetPhysicalDeviceSurfaceFormats(device, _surface, &formatCount, formats);
            
            for (var i = 0; i < formatCount; i++)
            {
                details.Formats[i] = formats[i];
            }
        }
        
        var presentModeCount = 0u;
        _vkSurface.GetPhysicalDeviceSurfacePresentModes(device, _surface, &presentModeCount, null);
        
        if (presentModeCount != 0)
        {
            details.PresentModes = new PresentModeKHR[presentModeCount];
            
            using var mem = GlobalMemory.Allocate((int) presentModeCount * sizeof(PresentModeKHR));
            var modes = (PresentModeKHR*) Unsafe.AsPointer(ref mem.GetPinnableReference());
            
            _vkSurface.GetPhysicalDeviceSurfacePresentModes(device, _surface, &presentModeCount, modes);
            
            for (var i = 0; i < presentModeCount; i++)
            {
                details.PresentModes[i] = modes[i];
            }
        }
        
        return details;
    }
    
    private unsafe bool CheckDeviceExtensionSupport(PhysicalDevice device)
    {
        return _deviceExtensions.All(ext => _vk.IsDeviceExtensionPresent(device, ext));
    }
    
    // Caching these values might have unintended side effects
    private unsafe QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
    {
        var indices = new QueueFamilyIndices();
        
        uint queryFamilyCount = 0;
        _vk.GetPhysicalDeviceQueueFamilyProperties(device, &queryFamilyCount, null);
        
        using var mem = GlobalMemory.Allocate((int) queryFamilyCount * sizeof(QueueFamilyProperties));
        var queueFamilies = (QueueFamilyProperties*) Unsafe.AsPointer(ref mem.GetPinnableReference());
        
        _vk.GetPhysicalDeviceQueueFamilyProperties(device, &queryFamilyCount, queueFamilies);
        for (var i = 0u; i < queryFamilyCount; i++)
        {
            var queueFamily = queueFamilies[i];
            // note: HasFlag is slow on .NET Core 2.1 and below.
            // if you're targeting these versions, use ((queueFamily.QueueFlags & QueueFlags.QueueGraphicsBit) != 0)
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                indices.GraphicsFamily = i;
            }
            
            _vkSurface.GetPhysicalDeviceSurfaceSupport(device, i, _surface, out var presentSupport);
            
            if (presentSupport == Silk.NET.Vulkan.Vk.True)
            {
                indices.PresentFamily = i;
            }
            
            if (indices.IsComplete())
            {
                break;
            }
        }
        
        return indices;
    }
    
    public struct QueueFamilyIndices
    {
        public uint? GraphicsFamily { get; set; }
        public uint? PresentFamily { get; set; }
        
        public bool IsComplete()
        {
            return GraphicsFamily.HasValue && PresentFamily.HasValue;
        }
    }
    
    public struct SwapChainSupportDetails
    {
        public SurfaceCapabilitiesKHR Capabilities { get; set; }
        public SurfaceFormatKHR[] Formats { get; set; }
        public PresentModeKHR[] PresentModes { get; set; }
    }
    
    private unsafe void CreateLogicalDevice()
    {
        var indices = FindQueueFamilies(_physicalDevice);
        var uniqueQueueFamilies = indices.GraphicsFamily.Value == indices.PresentFamily.Value
            ? new[] { indices.GraphicsFamily.Value }
            : new[] { indices.GraphicsFamily.Value, indices.PresentFamily.Value };
        
        _graphicsFamilyIndex = indices.GraphicsFamily.Value;
        
        using var mem = GlobalMemory.Allocate((int) uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
        var queueCreateInfos = (DeviceQueueCreateInfo*) Unsafe.AsPointer(ref mem.GetPinnableReference());
        
        var queuePriority = 1f;
        for (var i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            var queueCreateInfo = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueQueueFamilies[i],
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
            queueCreateInfos[i] = queueCreateInfo;
        }
        
        var deviceFeatures = new PhysicalDeviceFeatures();
        
        var createInfo = new DeviceCreateInfo();
        createInfo.SType = StructureType.DeviceCreateInfo;
        createInfo.QueueCreateInfoCount = (uint) uniqueQueueFamilies.Length;
        createInfo.PQueueCreateInfos = queueCreateInfos;
        createInfo.PEnabledFeatures = &deviceFeatures;
        createInfo.EnabledExtensionCount = (uint) _deviceExtensions.Count;
        
        var enabledExtensionNames = SilkMarshal.StringArrayToPtr(_deviceExtensions);
        createInfo.PpEnabledExtensionNames = (byte**) enabledExtensionNames;
        
        if (_validationLayersEnabled)
        {
            createInfo.EnabledLayerCount = (uint) _validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**) SilkMarshal.StringArrayToPtr(_validationLayers);
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
        }
        
        fixed (Device* device = &_device)
        {
            // todo - verify _physicalDeviceFeatures has what we need for best-practice implementation before creating device
            
            if (_vk.CreateDevice(_physicalDevice, &createInfo, null, device) != Result.Success)
            {
                throw new Exception("Failed to create logical device.");
            }
        }
        
        fixed (Queue* graphicsQueue = &_graphicsQueue)
        {
            _vk.GetDeviceQueue(_device, indices.GraphicsFamily.Value, 0, graphicsQueue);
        }
        
        fixed (Queue* presentQueue = &_presentQueue)
        {
            _vk.GetDeviceQueue(_device, indices.PresentFamily.Value, 0, presentQueue);
        }
        
        _vk.CurrentDevice = _device;
        
        if (!_vk.TryGetDeviceExtension(_instance, _device, out _vkSwapchain))
        {
            throw new NotSupportedException("KHR_swapchain extension not found.");
        }
        
        Console.WriteLine($"{_vk.CurrentInstance?.Handle} {_vk.CurrentDevice?.Handle}");
    }
    
    private unsafe void CreateSwapChain()
    {
        var swapChainSupport = QuerySwapChainSupport(_physicalDevice);
        
        var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
        var presentMode = ChooseSwapPresentMode(swapChainSupport.PresentModes);
        var extent = ChooseSwapExtent(swapChainSupport.Capabilities);
        
        var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
        if (swapChainSupport.Capabilities.MaxImageCount > 0 &&
            imageCount > swapChainSupport.Capabilities.MaxImageCount)
        {
            imageCount = swapChainSupport.Capabilities.MaxImageCount;
        }
        
        var createInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = _surface,
            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit
        };
        
        var indices = FindQueueFamilies(_physicalDevice);
        uint[] queueFamilyIndices = { indices.GraphicsFamily.Value, indices.PresentFamily.Value };
        
        fixed (uint* qfiPtr = queueFamilyIndices)
        {
            if (indices.GraphicsFamily != indices.PresentFamily)
            {
                createInfo.ImageSharingMode = SharingMode.Concurrent;
                createInfo.QueueFamilyIndexCount = 2;
                createInfo.PQueueFamilyIndices = qfiPtr;
            }
            else
            {
                createInfo.ImageSharingMode = SharingMode.Exclusive;
            }
            
            createInfo.PreTransform = swapChainSupport.Capabilities.CurrentTransform;
            createInfo.CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr;
            createInfo.PresentMode = presentMode;
            createInfo.Clipped = Silk.NET.Vulkan.Vk.True;
            
            createInfo.OldSwapchain = default;
            
            if (!_vk.TryGetDeviceExtension(_instance, _vk.CurrentDevice.Value, out _vkSwapchain))
            {
                throw new NotSupportedException("KHR_swapchain extension not found.");
            }
            
            fixed (SwapchainKHR* swapchain = &_swapchain)
            {
                if (_vkSwapchain.CreateSwapchain(_device, &createInfo, null, swapchain) != Result.Success)
                {
                    throw new Exception("failed to create swap chain!");
                }
            }
        }
        
        _vkSwapchain.GetSwapchainImages(_device, _swapchain, &imageCount, null);
        _swapchainImages = new Image[imageCount];
        fixed (Image* swapchainImage = _swapchainImages)
        {
            _vkSwapchain.GetSwapchainImages(_device, _swapchain, &imageCount, swapchainImage);
        }
        
        _swapchainImageFormat = surfaceFormat.Format;
        _swapchainExtent = extent;
    }
    
    private unsafe void RecreateSwapChain()
    {
        Vector2D<int> framebufferSize = _window.FramebufferSize;
        
        while (framebufferSize.X == 0 || framebufferSize.Y == 0)
        {
            framebufferSize = _window.FramebufferSize;
            _window.DoEvents();
        }
        
        _ = _vk.DeviceWaitIdle(_device);
        
        CleanupSwapchain();
        
        CreateSwapChain();
        CreateImageViews();
        CreateRenderPass();
        CreateGraphicsPipeline();
        CreateFramebuffers();
        CreateCommandBuffers();
        
        _imagesInFlight = new Fence[_swapchainImages.Length];
    }
    
    private Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            return capabilities.CurrentExtent;
        }
        
        var actualExtent = new Extent2D
            { Height = (uint) _window.FramebufferSize.Y, Width = (uint) _window.FramebufferSize.X };
        actualExtent.Width = new[]
        {
            capabilities.MinImageExtent.Width,
            new[] { capabilities.MaxImageExtent.Width, actualExtent.Width }.Min()
        }.Max();
        actualExtent.Height = new[]
        {
            capabilities.MinImageExtent.Height,
            new[] { capabilities.MaxImageExtent.Height, actualExtent.Height }.Min()
        }.Max();
        
        return actualExtent;
    }
    
    private PresentModeKHR ChooseSwapPresentMode(PresentModeKHR[] presentModes)
    {
        foreach (var availablePresentMode in presentModes)
        {
            if (availablePresentMode == PresentModeKHR.MailboxKhr)
            {
                return availablePresentMode;
            }
        }
        
        return PresentModeKHR.FifoKhr;
    }
    
    private SurfaceFormatKHR ChooseSwapSurfaceFormat(SurfaceFormatKHR[] formats)
    {
        foreach (var format in formats)
        {
            if (format.Format == Format.B8G8R8A8Unorm)
            {
                return format;
            }
        }
        
        return formats[0];
    }
    
    private unsafe void CreateImageViews()
    {
        _swapchainImageViews = new ImageView[_swapchainImages.Length];
        
        for (var i = 0; i < _swapchainImages.Length; i++)
        {
            var createInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = _swapchainImages[i],
                ViewType = ImageViewType.Type2D,
                Format = _swapchainImageFormat,
                Components =
                {
                    R = ComponentSwizzle.Identity,
                    G = ComponentSwizzle.Identity,
                    B = ComponentSwizzle.Identity,
                    A = ComponentSwizzle.Identity
                },
                SubresourceRange =
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                }
            };
            
            ImageView imageView = default;
            if (_vk.CreateImageView(_device, &createInfo, null, &imageView) != Result.Success)
            {
                throw new Exception("failed to create image views!");
            }
            
            _swapchainImageViews[i] = imageView;
        }
    }
    
    private unsafe void CreateRenderPass()
    {
        var colorAttachment = new AttachmentDescription
        {
            Format = _swapchainImageFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr
        };
        
        var colorAttachmentRef = new AttachmentReference
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal
        };
        
        var subpass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef
        };
        
        var dependency = new SubpassDependency
        {
            SrcSubpass = Silk.NET.Vulkan.Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            SrcAccessMask = 0,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            DstAccessMask = AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit
        };
        
        var renderPassInfo = new RenderPassCreateInfo
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 1,
            PAttachments = &colorAttachment,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = 1,
            PDependencies = &dependency
        };
        
        fixed (RenderPass* renderPass = &_renderPass)
        {
            if (_vk.CreateRenderPass(_device, &renderPassInfo, null, renderPass) != Result.Success)
            {
                throw new Exception("failed to create render pass!");
            }
        }
    }
    
   
    private unsafe void CreateGraphicsPipeline()
    {
        if (!VkCompiler.TryCompileShaderFile("./Vulkan/Silk.NET Lab/Shaders/triangle.vert.glsl", "main", out _, out var vertCompiled))
        {
            throw new Exception("Failed to compile vertex shader");
        }
        
        if (!VkCompiler.TryCompileShaderFile("./Vulkan/Silk.NET Lab/Shaders/triangle.frag.glsl", "main", out _, out var fragCompiled))
        {
            throw new Exception("Failed to compile fragment shader");
            
        }
        
        var vertShaderCode = vertCompiled; //Program.LoadEmbeddedResourceBytes($"{nameof(ImGuiVulkan)}.shader.vert.spv");
        var fragShaderCode = fragCompiled;//vertSrc; //Program.LoadEmbeddedResourceBytes($"{nameof(ImGuiVulkan)}.shader.frag.spv");
        var vertShaderModule = CreateShaderModule(vertShaderCode);
        var fragShaderModule = CreateShaderModule(fragShaderCode);
        
        var vertShaderStageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertShaderModule,
            PName = (byte*) SilkMarshal.StringToPtr("main")
        };
        
        var fragShaderStageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragShaderModule,
            PName = (byte*) SilkMarshal.StringToPtr("main")
        };
        
        var shaderStages = stackalloc PipelineShaderStageCreateInfo[2];
        shaderStages[0] = vertShaderStageInfo;
        shaderStages[1] = fragShaderStageInfo;
        
        var vertexInputInfo = new PipelineVertexInputStateCreateInfo
        {
            SType = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount = 0,
            VertexAttributeDescriptionCount = 0
        };
        
        var inputAssembly = new PipelineInputAssemblyStateCreateInfo
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = Silk.NET.Vulkan.Vk.False
        };
        
        var viewport = new Viewport
        {
            X = 0.0f,
            Y = 0.0f,
            Width = _swapchainExtent.Width,
            Height = _swapchainExtent.Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        
        var scissor = new Rect2D { Offset = default, Extent = _swapchainExtent };
        
        var viewportState = new PipelineViewportStateCreateInfo
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            PViewports = &viewport,
            ScissorCount = 1,
            PScissors = &scissor
        };
        
        var rasterizer = new PipelineRasterizationStateCreateInfo
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            DepthClampEnable = Silk.NET.Vulkan.Vk.False,
            RasterizerDiscardEnable = Silk.NET.Vulkan.Vk.False,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1.0f,
            CullMode = CullModeFlags.BackBit,
            FrontFace = FrontFace.Clockwise,
            DepthBiasEnable = Silk.NET.Vulkan.Vk.False
        };
        
        var multisampling = new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable = Silk.NET.Vulkan.Vk.False,
            RasterizationSamples = SampleCountFlags.Count1Bit
        };
        
        var colorBlendAttachment = new PipelineColorBlendAttachmentState
        {
            ColorWriteMask = ColorComponentFlags.RBit |
                             ColorComponentFlags.GBit |
                             ColorComponentFlags.BBit |
                             ColorComponentFlags.ABit,
            BlendEnable = Silk.NET.Vulkan.Vk.False
        };
        
        var colorBlending = new PipelineColorBlendStateCreateInfo
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = Silk.NET.Vulkan.Vk.False,
            LogicOp = LogicOp.Copy,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment
        };
        
        colorBlending.BlendConstants[0] = 0.0f;
        colorBlending.BlendConstants[1] = 0.0f;
        colorBlending.BlendConstants[2] = 0.0f;
        colorBlending.BlendConstants[3] = 0.0f;
        
        var pipelineLayoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 0,
            PushConstantRangeCount = 0
        };
        
        fixed (PipelineLayout* pipelineLayout = &_pipelineLayout)
        {
            if (_vk.CreatePipelineLayout(_device, &pipelineLayoutInfo, null, pipelineLayout) != Result.Success)
            {
                throw new Exception("failed to create pipeline layout!");
            }
        }
        
        var pipelineInfo = new GraphicsPipelineCreateInfo
        {
            SType = StructureType.GraphicsPipelineCreateInfo,
            StageCount = 2,
            PStages = shaderStages,
            PVertexInputState = &vertexInputInfo,
            PInputAssemblyState = &inputAssembly,
            PViewportState = &viewportState,
            PRasterizationState = &rasterizer,
            PMultisampleState = &multisampling,
            PColorBlendState = &colorBlending,
            Layout = _pipelineLayout,
            RenderPass = _renderPass,
            Subpass = 0,
            BasePipelineHandle = default
        };
        
        fixed (Pipeline* graphicsPipeline = &_graphicsPipeline)
        {
            if (_vk.CreateGraphicsPipelines
                    (_device, default, 1, &pipelineInfo, null, graphicsPipeline) != Result.Success)
            {
                throw new Exception("failed to create graphics pipeline!");
            }
        }
        
        _vk.DestroyShaderModule(_device, fragShaderModule, null);
        _vk.DestroyShaderModule(_device, vertShaderModule, null);
    }
    
    private unsafe ShaderModule CreateShaderModule(byte[] code)
    {
        var createInfo = new ShaderModuleCreateInfo
        {
            SType = StructureType.ShaderModuleCreateInfo,
            CodeSize = (nuint) code.Length
        };
        fixed (byte* codePtr = code)
        {
            createInfo.PCode = (uint*) codePtr;
        }
        
        var shaderModule = new ShaderModule();
        if (_vk.CreateShaderModule(_device, &createInfo, null, &shaderModule) != Result.Success)
        {
            throw new Exception("failed to create shader module!");
        }
        
        return shaderModule;
    }
    
    private unsafe void CreateFramebuffers()
    {
        _swapchainFramebuffers = new Framebuffer[_swapchainImageViews.Length];
        
        for (var i = 0; i < _swapchainImageViews.Length; i++)
        {
            var attachment = _swapchainImageViews[i];
            var framebufferInfo = new FramebufferCreateInfo
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = _renderPass,
                AttachmentCount = 1,
                PAttachments = &attachment,
                Width = _swapchainExtent.Width,
                Height = _swapchainExtent.Height,
                Layers = 1
            };
            
            var framebuffer = new Framebuffer();
            if (_vk.CreateFramebuffer(_device, &framebufferInfo, null, &framebuffer) != Result.Success)
            {
                throw new Exception("failed to create framebuffer!");
            }
            
            _swapchainFramebuffers[i] = framebuffer;
        }
    }
    
    private unsafe void CreateCommandPool()
    {
        var queueFamilyIndices = FindQueueFamilies(_physicalDevice);
        
        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamilyIndices.GraphicsFamily.Value,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit
        };
        
        fixed (CommandPool* commandPool = &_commandPool)
        {
            if (_vk.CreateCommandPool(_device, &poolInfo, null, commandPool) != Result.Success)
            {
                throw new Exception("failed to create command pool!");
            }
        }
    }
    
    private unsafe void CreateCommandBuffers()
    {
        _commandBuffers = new CommandBuffer[_swapchainFramebuffers.Length];
        
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint) _commandBuffers.Length
        };
        
        fixed (CommandBuffer* commandBuffers = _commandBuffers)
        {
            if (_vk.AllocateCommandBuffers(_device, &allocInfo, commandBuffers) != Result.Success)
            {
                throw new Exception("failed to allocate command buffers!");
            }
        }
    }
    
    private unsafe void CreateSyncObjects()
    {
        _imageAvailableSemaphores = new Semaphore[MaxFramesInFlight];
        _renderFinishedSemaphores = new Semaphore[MaxFramesInFlight];
        _inFlightFences = new Fence[MaxFramesInFlight];
        _imagesInFlight = new Fence[MaxFramesInFlight];
        
        SemaphoreCreateInfo semaphoreInfo = new SemaphoreCreateInfo();
        semaphoreInfo.SType = StructureType.SemaphoreCreateInfo;
        
        FenceCreateInfo fenceInfo = new FenceCreateInfo();
        fenceInfo.SType = StructureType.FenceCreateInfo;
        fenceInfo.Flags = FenceCreateFlags.SignaledBit;
        
        for (var i = 0; i < MaxFramesInFlight; i++)
        {
            Semaphore imgAvSema, renderFinSema;
            Fence inFlightFence;
            if (_vk.CreateSemaphore(_device, &semaphoreInfo, null, &imgAvSema) != Result.Success ||
                _vk.CreateSemaphore(_device, &semaphoreInfo, null, &renderFinSema) != Result.Success ||
                _vk.CreateFence(_device, &fenceInfo, null, &inFlightFence) != Result.Success)
            {
                throw new Exception("failed to create synchronization objects for a frame!");
            }
            
            _imageAvailableSemaphores[i] = imgAvSema;
            _renderFinishedSemaphores[i] = renderFinSema;
            _inFlightFences[i] = inFlightFence;
        }
    }
}
