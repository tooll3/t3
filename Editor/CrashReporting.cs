using System.IO;
using System.Management;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
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
                           o.SetBeforeSend((Func<SentryEvent, SentryHint, SentryEvent>)CrashHandler);
                       });

        //SentrySdk.ConfigureScope(scope => { scope.SetTag("IsStandAlone", Program.IsStandAlone ? "Yes" : "No"); });

        var configuration = "Release";
        #if DEBUG
        configuration = "Debug";
        #endif

        SentrySdk.ConfigureScope(scope => { scope.SetTag("Configuration", configuration); });
    }

    private static SentryEvent CrashHandler(SentryEvent sentryEvent, SentryHint hint)
    {
        var timeOfLastBackup = AutoBackup.GetTimeOfLastBackup();
        var timeSpan = THelpers.GetReadableRelativeTime(timeOfLastBackup);

        var canvas = GraphWindow.Focused?.GraphCanvas;
        
        sentryEvent.SetTag("Nickname", UserSettings.Config.UserName);
        sentryEvent.Contexts["tooll3"]= new
                                            {
                                                UndoStack = UndoRedoStack.GetUndoStackAsString(),
                                                Selection = canvas == null ? string.Empty : string.Join("\n", canvas.NodeSelection),
                                                Nickname = "",
                                                RuntimeSeconds = Playback.RunTimeInSecs,
                                                RuntimeFrames = ImGui.GetFrameCount(),
                                                UndoActions = UndoRedoStack.UndoStack.Count,
                                            };
        
        try
        {
            var primaryComposition = GraphWindow.Focused?.CompositionOp;
            if (primaryComposition != null)
            {
                var compositionUi = primaryComposition.Symbol.GetSymbolUi();
                var json = GraphOperations.CopyNodesAsJson(
                                                           primaryComposition,
                                                           compositionUi.ChildUis.Values,
                                                           compositionUi.Annotations.Values.ToList());
                EditorUi.Instance.SetClipboardText(json);
            }
        }
        catch (Exception e)
        {
            sentryEvent.SetExtra("CurrentOpExportFailed", e.Message);
        }

        WriteCrashReportFile(sentryEvent);

        // We only show crash report dialog in release mode 
        #if RELEASE
        var result = CoreUi.Instance.ShowMessageBox(string.Join("\n",
                                                                "Oh noooo, how embarrassing! T3 just crashed.",
                                                                $"Last backup was saved {timeSpan} to .t3/backups/",
                                                                "We copied the current operator to your clipboard.",
                                                                "Please read the Wiki on what to do next.",
                                                                "",
                                                                "Click Yes to send a crash report to tooll.sentry.io.",
                                                                "This will hopefully help us to fix this issue."
                                                               ),
                                                    @"☠🙈 Damn!",
                                                    PopUpButtons.YesNo);

        var sendingEnabled = result == PopUpResult.Yes;

        if (!string.IsNullOrWhiteSpace(LogPath))
        {
            CoreUi.Instance.OpenWithDefaultApplication(LogPath);
        }
        
        return sendingEnabled ? sentryEvent : null;
        #else
        WriteReportToLog(sentryEvent, false);
        CoreUi.Instance.SetUnhandledExceptionMode(true);
        return null;
        #endif
    }
    
    private static void WriteReportToLog(SentryEvent sentryEvent, bool sendingEnabled)
    {
        // Write formatted stacktrace for readability
        if (sentryEvent.Exception != null)
        {
            Log.Warning($"{sentryEvent.Exception.Message}\n{sentryEvent.Exception}");
        }

        // Dump report as json
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        sentryEvent.WriteTo(writer, null);
        writer.Flush();

        var eventJson = Encoding.UTF8.GetString(stream.ToArray());
        Log.Warning($"Encountered crash reported:{sendingEnabled}\n" + eventJson);
        
        // Force writing
        FileWriter.Flush();
    }
    
    /** Additional to logging the crash we also write a copy to a dedicated crash file. */
    private static void WriteCrashReportFile(SentryEvent sentryEvent)
    {
        if (sentryEvent?.Exception == null)
            return;
        
        var exceptionTitle = sentryEvent.Exception.GetType().Name;
        var filepath= Path.Combine(FileWriter.Instance.LogDirectory,$@"crash {DateTime.Now:yyyy-MM-dd  HH-mm-ss} - {exceptionTitle}.txt");
        
        using var streamFileWriter = new StreamWriter(filepath);
        streamFileWriter.WriteLine($"{sentryEvent.Exception.Message}\n{sentryEvent.Exception}");
        
        using var memoryStream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true });

        sentryEvent.WriteTo(jsonWriter, null);
        jsonWriter.Flush();

        streamFileWriter.WriteLine(Encoding.UTF8.GetString(memoryStream.ToArray()));
        streamFileWriter.Flush();
    }

    public static string LogPath { get; set; }
}