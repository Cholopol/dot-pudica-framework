using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace DotPudica.SourceGenerator;

/// <summary>
/// View class information extracted from syntax tree that requires code generation.
/// </summary>
internal sealed class ViewClassInfo
{
    /// <summary>Full namespace of View class</summary>
    public string Namespace { get; set; } = "";

    /// <summary>View class name</summary>
    public string ClassName { get; set; } = "";

    /// <summary>Generic ViewModel type parameter name (e.g., LoginViewModel)</summary>
    public string ViewModelTypeName { get; set; } = "";

    /// <summary>Whether the class already overrides _Ready().</summary>
    public bool HasReadyOverride { get; set; }

    /// <summary>Whether the class already overrides _ExitTree().</summary>
    public bool HasExitTreeOverride { get; set; }

    /// <summary>Whether this class declares the DotPudica view attribute itself and owns the runtime members.</summary>
    public bool OwnsDotPudicaRuntime { get; set; }

    /// <summary>All fields marked with [BindTo]</summary>
    public List<PropertyBindingInfo> PropertyBindings { get; } = new();

    /// <summary>All fields marked with [BindCommand]</summary>
    public List<CommandBindingInfo> CommandBindings { get; } = new();
}

/// <summary>
/// Description of a single [BindTo] binding.
/// </summary>
internal sealed class PropertyBindingInfo
{
    /// <summary>Field name (e.g., usernameInput)</summary>
    public string FieldName { get; set; } = "";

    /// <summary>Godot control type short name (e.g., LineEdit)</summary>
    public string ControlType { get; set; } = "";

    /// <summary>ViewModel property path (e.g., "Account.Username")</summary>
    public string SourcePath { get; set; } = "";

    /// <summary>Binding mode (e.g., "BindingMode.TwoWay")</summary>
    public string BindingMode { get; set; } = "DotPudica.Core.Binding.BindingMode.Default";

    /// <summary>Target control property name (e.g., "Text"), null to infer from control type</summary>
    public string? TargetProperty { get; set; }

    /// <summary>Control change signal name (e.g., "text_changed"), null to infer from control type</summary>
    public string? SourceEvent { get; set; }

    /// <summary>Value converter type name (e.g., "BoolNegateConverter"), null if not used</summary>
    public string? ConverterType { get; set; }
}

/// <summary>
/// Description of a single [BindCommand] binding.
/// </summary>
internal sealed class CommandBindingInfo
{
    /// <summary>Field name (e.g., loginBtn)</summary>
    public string FieldName { get; set; } = "";

    /// <summary>Godot control type short name (e.g., Button)</summary>
    public string ControlType { get; set; } = "";

    /// <summary>ICommand property name on ViewModel (e.g., "LoginCommand")</summary>
    public string CommandName { get; set; } = "";

    /// <summary>Command parameter path (optional)</summary>
    public string? ParameterPath { get; set; }

    /// <summary>Trigger signal (default "pressed")</summary>
    public string Signal { get; set; } = "pressed";
}
