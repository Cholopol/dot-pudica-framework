using System.Collections.Generic;
using Godot;

namespace DotPudica.Integration.Controls;

/// <summary>
/// Lightweight control that records the thread ID of Text writes. Used via DelegateTargetProxy / property delegate binding,
/// to assert that binding target updates occur on the Godot main thread.
/// </summary>
public partial class ThreadTrackingControl : Control
{
    private string _text = "";

    public List<int> SetThreadIds { get; } = new();

    public int AccessCount { get; private set; }

    public string Text
    {
        get => _text;
        set
        {
            AccessCount++;
            SetThreadIds.Add(System.Environment.CurrentManagedThreadId);
            _text = value ?? "";
        }
    }
}
