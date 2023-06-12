using System;
using System.Collections.Generic;
using T3.Core.Operator;

namespace T3.Editor.Gui.Graph.Interaction;

/// <summary>
/// Manage the navigation between previously selected instances.
/// This can also be used sort items by relevance in search dialog.
/// </summary>
static class NavigationHistory
{
        
    /// <summary>
    /// abC  (select D)
    /// abcD (select C)
    /// abCd (select A)
    /// bcdA
    /// 
    /// </summary>
    private static void UpdateSelectedInstance(Instance instance)
    {
            
    }

    private static Instance GetPreviousNavigationInstance()
    {
        return null;
    }

    private static Instance GetNextNavigationInstance()
    {
        return null;
    }
        
        
    private static List<List<Guid>> _previousSelections = new();
}