using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Utils;

namespace T3.Core.Stats;

/// <summary>
/// Collect status about failing instances. This can then be used to expose problems withing nested Instances.
/// </summary>
public static class OperatorDiagnostics
{
    public static void LogWarningState(this Instance instance, string message)
    {
        Logging.Log.Warning(message, instance);
        KeepState(instance,message,Statuses.HasWarning);
    }    
    
    public static void LogErrorState(this Instance instance, string message)
    {
        Logging.Log.Error(message, instance);
        KeepState(instance, message, Statuses.HasError);
    }


    public static void ClearErrorState(this Instance instance)
    {
        var hash = OperatorUtils.ComputeInstanceHash(instance.InstancePath);
        if (!StatusUpdates.Remove(hash, out var lastStatus))
        {
        }
    }

    public static readonly ConcurrentDictionary<long, StatusUpdate> StatusUpdates = [];

    private static void KeepState(Instance instance, string message, Statuses status)
    {
        var hash = OperatorUtils.ComputeInstanceHash(instance.InstancePath);
        StatusUpdates[hash]=new StatusUpdate(
                                             hash, 
                                             instance.InstancePath, 
                                             message, 
                                             status,
                                             Playback.RunTimeInSecs);
        LastChangeTime = Playback.RunTimeInSecs;
    }

    public static double LastChangeTime { get; private set; }
    
    public enum Statuses
    {
        Unknown = 0,
        Okay = 1,
        HasWarning = 2,
        HasError = 3,
    }
    
    public sealed record StatusUpdate(
        long InstanceHash,
        IReadOnlyList<Guid> IdPath,
        string Message,
        Statuses Statuses,
        double Time);
    
}