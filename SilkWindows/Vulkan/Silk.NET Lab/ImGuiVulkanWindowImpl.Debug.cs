// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Vulkan;

namespace SilkWindows.Vulkan.Silk.NET_Lab;

public sealed partial class ImGuiVulkanWindowImpl
{
    private unsafe void SetupDebugMessenger()
    {
        if (!_validationLayersEnabled) return;
        if (!_vk.TryGetInstanceExtension(_instance, out _debugUtils))
        {
            Console.WriteLine("Debug utils extension not found.");
            return;
        }
        
        var createInfo = new DebugUtilsMessengerCreateInfoEXT
                             {
                                 SType = StructureType.DebugUtilsMessengerCreateInfoExt,
                                 MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                                   DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                                   DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt |
                                                   DebugUtilsMessageSeverityFlagsEXT.InfoBitExt,
                                                   //DebugUtilsMessageSeverityFlagsEXT.None,
                                 MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                               DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                               DebugUtilsMessageTypeFlagsEXT.ValidationBitExt |
                                               DebugUtilsMessageTypeFlagsEXT.DeviceAddressBindingBitExt,
                                               //DebugUtilsMessageTypeFlagsEXT.None,
                                 PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT) DebugCallback
                             };
        
        fixed (DebugUtilsMessengerEXT* debugMessenger = &_debugMessenger)
        {
            if (_debugUtils.CreateDebugUtilsMessenger
                    (_instance, &createInfo, null, debugMessenger) != Result.Success)
            {
                throw new Exception("Failed to create debug messenger.");
            }
        }
        
    }
    
    
    private static readonly unsafe DebugUtilsMessengerCallbackFunctionEXT DebugCallbackDelegate = DebugCallback;
    private static readonly PfnDebugUtilsMessengerCallbackEXT DebugCallbackPfn = new(DebugCallbackDelegate);
    
    [StackTraceHidden]
    private static unsafe uint DebugCallback
    (
        DebugUtilsMessageSeverityFlagsEXT messageSeverity,
        DebugUtilsMessageTypeFlagsEXT messageTypes,
        DebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* pUserData
    )
    {
        if (messageSeverity > DebugUtilsMessageSeverityFlagsEXT.InfoBitExt)
        {
            const string separator = "---------------------------------------------------";
            var sb = new StringBuilder(2048);
            sb.AppendLine(separator);
            sb.Append($"{messageSeverity} {messageTypes}");
            sb.AppendLine(Marshal.PtrToStringAnsi((nint) pCallbackData->PMessage));
            sb.AppendLine();
            
            const int frameCount = 5;
            const int skipCount = 1;
            
            var stackTrace = new StackTrace(skipCount, true);
            var maxFrames = Math.Min(stackTrace.FrameCount, frameCount);
            
            for (int i = 0; i < maxFrames; i++)
            {
                var frame = stackTrace.GetFrame(i);
                sb.Append("\tat ");
                sb.Append('\"');
                sb.Append(frame!.GetMethod()?.Name ?? "<unknown>");
                sb.Append('\"');
                sb.Append(" in ");
                sb.Append(frame.GetFileName() ?? "<unknown>");
                sb.Append(':');
                sb.Append(frame.GetFileLineNumber());
                sb.AppendLine();
            }
            
            sb.Append(separator);
            Console.Write(sb.ToString());
        }
        
        return Vk.True;
    }
    
    private unsafe void CheckValidationLayerSupport()
    {
        if (ValidationMode == ValidationModes.None)
            #pragma warning disable CS0162 // Unreachable code detected
        {
            _validationLayersEnabled = false;
            return;
        }
        #pragma warning restore CS0162 // Unreachable code detected
        
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
    
    private bool _validationLayersEnabled;
    private const ValidationModes ValidationMode = ValidationModes.Requested;
    
    private string[] _validationLayers = ["VK_LAYER_KHRONOS_validation"];
}
