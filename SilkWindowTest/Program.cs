using System.Numerics;
using SilkWindows;
using SilkWindows.Implementations.FileManager;

var uiHandler = new SilkBlockingDialog();

ManagedDirectory[] directories = [new ManagedDirectory("C:/Users/Dom/Desktop", true, "Your damn desktop"), new ManagedDirectory("C:/Users/Dom/AppData/Roaming", true), new ManagedDirectory("C:/Users/Dom/Downloads", false)];
var path = uiHandler.Show(title: "File dropper",
                          drawer: new FileManager(FileManagerMode.PickFile, directories),
                          options: new SimpleWindowOptions(Vector2.One * 600, 60, true, true, true));

Console.WriteLine($"Got \"{path}\"");