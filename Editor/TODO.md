## Refactor project handling 

x Remove GraphWindow.CanOpenAnotherWindow
x Prevent last project window from being closed
x Show project after NewProjectDialog -> ProjectManager.TryOpenInstancePath().
x Test AppMenu -> New Graph Window

- fix view regions when navigating in/out
- unload projects from project list
x somewhat presentable project list

- Load last project from user settings
- focus graph imgui-window after creation 