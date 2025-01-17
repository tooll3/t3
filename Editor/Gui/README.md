# Discussing Project Views

2025-01-17

- Ⓢ **ProjectManager**
    - **OpenedProjects[]**
        - **Structure** – an instance tree with `rootInstance`
        - **Package**
    - `LoadProject()`
    - `TryOpenInstancePath()`

- **Windows** _(with layout)_
    - **GraphWindows[]**
        - **ProjectView?** – References the window’s UI model
        - Ⓢ **ProjectListView** – Shown if `!ProjectView`

- ~~GraphComponents~~ → **ProjectView** – Contains all components required to render a graph view
    - Ⓢ **.Focused** – Access to the current (primary) graph view and its structure
    - **.IsFocused**
    - **.OpenedProject**
    - **.Composition**
    - **.GraphCanvas** `<Legacy|Magnetic>`
    - **.Timeline**
    - **.NavigationHistory**
    - **.NodeSelection**
    - **.GraphImageBackground**

Ⓢ - a static class

## Notes

### **On _Windows_, _GraphEditor_, and _OpenProjects_**
The content of a `GraphWindow` can be switched to an **OpenProject** and its structure via `GraphWindow.ViewProject(OpenProject, InstancePath?)`. This discards all previous components and initializes a new `ProjectView`, including _NavigationHistory_.

Navigation within a structure is handled by `ProjectView.Canvas.SetComposition(InstancePath)`.

### **Focused ProjectView**
In most scenarios, there will be a focused `ProjectView`. Only if all project views are closed or the user is in the **ProjectList** will the focused view be `null`. In this case, some windows (such as the **ParameterWindow** and **SymbolLibrary**) will no longer be able to display meaningful content and _must_ indicate this with a message like **"No project active."**

### **Switching Between OpenProjects**
It is assumed that `ProjectViews` do not hold critical information and can be created and discarded efficiently _(without frame drops)_.

While users will typically work in a **single GraphWindow**, various components might reference instances in other open projects (e.g., an operator pinned in an Output Window or a reference in the console log). It may be desirable to switch the `GraphWindow` to the referenced project and instance via a method like: `ProjectManager.TryOpenInstancePath()`  

This method would attempt to focus the instance in the following order:
1. **If the project matches** – Focus the instance within the currently focused `ProjectView`.
2. **If another ProjectView exists** – Switch to that view if it contains the requested project.
3. **If neither exists** – Switch the focused view to the matching project.
4. **If the project is not loaded** – Load the project and switch the focused view to it.

If the `InstancePath` is no longer valid, it should attempt to open as much of the path as possible before displaying an error (popup?) message. For example, it should open the last valid composition that previously contained the requested `SymbolChildId`.

This behavior could potentially interfere with _NavigationHistory_.

Instance paths should always contain the project package.

### **On Closing Projects**
The `ProjectManager` will be notified when `ProjectViews` are discarded and may unload the project if no active views remain. However, this could lead to unintended data loss.

An alternative approach is to indicate loaded projects in the **ProjectViewList** and provide an explicit **Close** button there.

If a `ProjectView`’s **OpenProject** is closed, that `ProjectView` should also be discarded, and its `GraphWindow` should switch to the **ProjectList** view.