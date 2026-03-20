using System;

namespace DotPudica.Godot.Views;

/// <summary>
/// Marks a Godot script stub as a DotPudica MVVM view and declares its ViewModel type.
/// The actual MVVM members are injected by the DotPudica source generator.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class DotPudicaViewAttribute : Attribute
{
    public DotPudicaViewAttribute(Type viewModelType)
    {
        ViewModelType = viewModelType;
    }

    public Type ViewModelType { get; }
}
