using System;
using DotPudica.Core.ViewModels;

namespace DotPudica.Godot.Views;

/// <summary>
/// Marks a Godot script stub as a DotPudica MVVM view and declares its ViewModel type.
/// The actual MVVM members (runtime, lifecycle, bindings) are injected by the DotPudica source generator.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class DotPudicaViewAttribute : Attribute
{
    public DotPudicaViewAttribute(Type viewModelType)
    {
        ViewModelType = viewModelType;
    }

    public Type ViewModelType { get; }

    /// <summary>
    /// ViewModel ownership relative to this view. Defaults to <see cref="ViewModelOwnership.Owned"/>:
    /// the view creates the instance and disposes it on teardown.
    /// Use <see cref="ViewModelOwnership.External"/> when the ViewModel is created elsewhere
    /// (shared panels) — combined with <see cref="AutoInitialize"/> = false and a manual
    /// <c>SetViewModel(vm, ViewModelOwnership.External)</c> call.
    /// </summary>
    public ViewModelOwnership Ownership { get; set; } = ViewModelOwnership.Owned;

    /// <summary>
    /// When true (default), the source generator emits the full lifecycle: service injection,
    /// ViewModel creation, SetViewModel, DotPudicaInitialize, event subscriptions and dispose.
    /// Set to false for views that bind on demand (e.g., shared panels calling a public
    /// BindShared method): the generator then skips ViewModel creation and initialization,
    /// but still emits the teardown (_ExitTree → dispose).
    /// </summary>
    public bool AutoInitialize { get; set; } = true;

    /// <summary>
    /// When true, the view is poolable: the generator emits <c>RecycleView()</c> (unbind +
    /// unsubscribe, node survives; user must override <c>_ExitTree() => RecycleView();</c>) and,
    /// with <see cref="AutoInitialize"/> = false, <c>ActivateViewModel(vm)</c>. Recycling calls
    /// <c>RequestReady()</c>, so the node re-runs <c>_Ready() => InitializeView()</c> on the next
    /// tree entry (Godot calls <c>_ready()</c> once per node instance).
    /// </summary>
    public bool Pooled { get; set; }
}
