using System;
using System.Linq;
using System.Management;
using ImGuiNET;
using Sentry;
using T3.Core.Animation;
using T3.Core.SystemUi;
using T3.Editor.Gui.AutoBackup;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.SystemUi;
using T3.Editor.UiModel;
using T3.SystemUi;

namespace T3.Editor;

internal static class CrashReporting
{
    public static void InitializeCrashReporting()
    {
        SentrySdk.Init(o =>
                       {
                           // Tells which project in Sentry to send events to:
                           o.Dsn = "https://52e37e10dc9cebcc2328cc63fab57211@o4505681078059008.ingest.sentry.io/4505681082384384";
                           o.Debug = false;
                           o.TracesSampleRate = 0.0;
                           o.IsGlobalModeEnabled = true;
                           o.SendClientReports = false;
                           o.AutoSessionTracking = false;
                           o.SendDefaultPii = false;
                           o.Release = Program.GetReleaseVersion(indicateDebugBuild: false);
                           o.SetBeforeSend((Func<SentryEvent, Hint, SentryEvent>)CrashHandler);
                       });

        //SentrySdk.ConfigureScope(scope => { scope.SetTag("IsStandAlone", Program.IsStandAlone ? "Yes" : "No"); });

        var configuration = "Release";
        #if DEBUG
                configuration = "Debug";
        #endif

        SentrySdk.ConfigureScope(scope => { scope.SetTag("Configuration", configuration); });
    }

    private static SentryEvent CrashHandler(SentryEvent sentryEvent, Hint hint)
    {
        var timeOfLastBackup = AutoBackup.GetTimeOfLastBackup();
        var timeSpan = THelpers.GetReadableRelativeTime(timeOfLastBackup);

        CoreUi.Instance.SetUnhandledExceptionMode(true);
        var result = CoreUi.Instance.ShowMessageBox(string.Join("\n",
                                                 "Oh noooo, how embarrassing! T3 just crashed.",
                                                 $"Last backup was saved {timeSpan} to .t3/backups/",
                                                 "We copied the current operator to your clipboard.",
                                                 "Check the FAQ on what to do next.",
                                                 "",
                                                 "Click Yes to send a crash report to tooll.sentry.io.",
                                                 "This will hopefully help us to fix this issue."
                                                ),
                                     @"☠🙈 Damn!",
                                     PopUpButtons.YesNo);


        sentryEvent.SetTag("Nickname", "anonymous");
        sentryEvent.Contexts["tooll3"]= new
                                            {
                                                UndoStack = UndoRedoStack.GetUndoStackAsString(),
                                                Selection = string.Join("\n", NodeSelection.Selection),
                                                Nickname = "",
                                                RuntimeSeconds = Playback.RunTimeInSecs,
                                                RuntimeFrames = ImGui.GetFrameCount(),
                                                UndoActions = UndoRedoStack.UndoStack.Count,
                                            };
        
        try
        {
            var primaryComposition = GraphWindow.GetMainComposition();
            if (primaryComposition != null)
            {
                var compositionUi = SymbolUiRegistry.Entries[primaryComposition.Symbol.Id];
                var json = GraphOperations.CopyNodesAsJson(
                                                           primaryComposition.Symbol.Id,
                                                           compositionUi.ChildUis,
                                                           compositionUi.Annotations.Values.ToList());
                EditorUi.Instance.SetClipboardText(json);
            }
        }
        catch (Exception e)
        {
            sentryEvent.SetExtra("CurrentOpExportFailed", e.Message);
        }

        return result == PopUpResult.Yes ? sentryEvent : null;
    }

    private static void GetGraphicsCardAdapter()
    {
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
        foreach (ManagementObject mo in searcher.Get())
        {
            PropertyData currentBitsPerPixel = mo.Properties["CurrentBitsPerPixel"];
            PropertyData description = mo.Properties["Description"];
            if (currentBitsPerPixel != null && description != null)
            {
                if (currentBitsPerPixel.Value != null)
                    System.Console.WriteLine(description.Value);
            }
        }
        //IDirect3D9::GetAdapterIdentifier

    }
    
}