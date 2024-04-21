// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Silk.NET.Vulkan;

namespace ImGuiVulkan;

public partial class ImGuiVulkanWindowImpl
{
    private unsafe void SetupDebugMessenger()
    {
        if (!_validationLayersEnabled) return;
        if (!_vk.TryGetInstanceExtension(_instance, out _debugUtils)) return;
        
        var createInfo = new DebugUtilsMessengerCreateInfoEXT();
        PopulateDebugMessengerCreateInfo(ref createInfo);
        
        fixed (DebugUtilsMessengerEXT* debugMessenger = &_debugMessenger)
        {
            if (_debugUtils.CreateDebugUtilsMessenger
                    (_instance, &createInfo, null, debugMessenger) != Result.Success)
            {
                throw new Exception("Failed to create debug messenger.");
            }
        }
    }
    
    private unsafe void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
    {
        createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
        createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
        createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT) DebugCallback;
    }
    
    private unsafe uint DebugCallback
    (
        DebugUtilsMessageSeverityFlagsEXT messageSeverity,
        DebugUtilsMessageTypeFlagsEXT messageTypes,
        DebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* pUserData
    )
    {
        if (messageSeverity > DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt)
        {
            Console.WriteLine
                ($"{messageSeverity} {messageTypes}" + Marshal.PtrToStringAnsi((nint) pCallbackData->PMessage));
        }
        
        return Silk.NET.Vulkan.Vk.False;
    }
    
    private unsafe void CheckValidationLayerSupport()
    {
        if (ValidationMode == ValidationModes.None)
        {
            _validationLayersEnabled = false;
            return;
        }
        
        uint layerCount = 0;
        _vk.EnumerateInstanceLayerProperties(&layerCount, (LayerProperties*) 0);
        
        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* availableLayersPtr = availableLayers)
            _vk.EnumerateInstanceLayerProperties(&layerCount, availableLayersPtr);
        
        var availableLayerNames = new List<string>();
        foreach (var layerProperties in availableLayers)
        {
            availableLayerNames.Add(Marshal.PtrToStringAnsi((nint) layerProperties.LayerName));
        }
        
        switch (ValidationMode)
        {
            case ValidationModes.Requested:
            {
                foreach (var layer in _validationLayers)
                {
                    if (!availableLayerNames.Contains(layer))
                    {
                        Console.WriteLine($"Failed to find validation layer \"{layer}\"");
                    }
                }
                
                break;
            }
            case ValidationModes.None:
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        var validationLayersToUse = availableLayerNames
            .Where(x => _validationLayers.Contains(x))
            .ToArray();
        
        foreach (var layer in _validationLayers)
        {
            var hasLayer = validationLayersToUse.Contains(layer);
            var log = hasLayer
                ? "Validation layer \"{0}\" included"
                : "Failed to find layer \"{0}\". Do you have the Vulkan SDK installed?";
            Console.WriteLine(log, layer);
        }
        
        _validationLayers = validationLayersToUse.ToArray();
        _validationLayersEnabled = _validationLayers.Length > 0;
    }
}
