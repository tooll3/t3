using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using ImGuiNET;
using Sentry;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Editor.Gui.AutoBackup;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.SystemUi;
using T3.Editor.UiModel;

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

        SentrySdk.ConfigureScope(scope => { scope.SetTag("IsStandAlone", Program.IsStandAlone ? "Yes" : "No"); });

        var configuration = "Release";
        #if DEBUG
        configuration = "Debug";
        #endif

        SentrySdk.ConfigureScope(scope => { scope.SetTag("Configuration", configuration); });

        // Configure WinForms to throw exceptions so Sentry can capture them.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
    }

    private static SentryEvent CrashHandler(SentryEvent sentryEvent, Hint hint)
    {
        var timeOfLastBackup = AutoBackup.GetTimeOfLastBackup();
        var timeSpan = THelpers.GetReadableRelativeTime(timeOfLastBackup);

        var result = MessageBox.Show(string.Join("\n",
                                                 "Oh noooo, how embarrassing! T3 just crashed.",
                                                 $"Last backup was saved {timeSpan} to .t3/backups/",
                                                 "We copied the current operator to your clipboard.",
                                                 "Please read the Wiki on what to do next.",
                                                 "",
                                                 "Click Yes to send a crash report to tooll.sentry.io.",
                                                 "This will hopefully help us to fix this issue."
                                                ),
                                     @"☠🙈 Damn!",
                                     MessageBoxButtons.YesNo);

        var sendingEnabled = result == DialogResult.Yes;

        sentryEvent.SetTag("Nickname", UserSettings.Config?.UserName ?? "anonymous");
        sentryEvent.Contexts["tooll3"] = new
                                             {
                                                 UndoStack = UndoRedoStack.GetUndoStackAsString(),
                                                 Selection = string.Join("\n", NodeSelection.Selection),
                                                 Nickname = UserSettings.Config?.UserName ?? "",
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

        WriteReportToLog(sentryEvent, sendingEnabled);
        WriteCrashReportFile(sentryEvent);
        return sendingEnabled ? sentryEvent : null;
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
        Directory.CreateDirectory(FileWriter.LogDirectory);
        var filepath= ($@"{FileWriter.LogDirectory}/crash {exceptionTitle} - {DateTime.Now:yyyy-MM-dd  HH-mm-ss}.txt");
        
        using var streamFileWriter = new StreamWriter(filepath);
        streamFileWriter.WriteLine($"{sentryEvent.Exception.Message}\n{sentryEvent.Exception}");
        
        using var memoryStream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true });

        sentryEvent.WriteTo(jsonWriter, null);
        jsonWriter.Flush();

        streamFileWriter.WriteLine(Encoding.UTF8.GetString(memoryStream.ToArray()));
        streamFileWriter.Flush();
    }
}