using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace DotPudica.SourceGenerator;

internal sealed class ViewClassInfo
{
    public string Namespace { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string ViewModelTypeName { get; set; } = "";
    public string BaseTypeDisplay { get; set; } = "";
    public INamedTypeSymbol? ViewModelSymbol { get; set; }
    public bool HasReadyOverride { get; set; }
    public bool HasExitTreeOverride { get; set; }
    public bool CallsInitializeView { get; set; }
    public bool CallsDisposeView { get; set; }
    public bool CallsRecycleView { get; set; }
    public bool OwnsDotPudicaRuntime { get; set; }
    public Location? Location { get; set; }
    public List<PropertyBindingInfo> PropertyBindings { get; set; } = new();
    public List<CommandBindingInfo> CommandBindings { get; set; } = new();
    public List<CollectionBindingInfo> CollectionBindings { get; set; } = new();
    public string OwnershipExpression { get; set; } = "DotPudica.Core.ViewModels.ViewModelOwnership.Owned";
    public bool AutoInitialize { get; set; } = true;
    public bool Pooled { get; set; }
    public bool HasFactoryMethod { get; set; }
    public bool HasFactoryDeclaration { get; set; }
    public string FactoryMethodName { get; set; } = "";
    public string? ViewModelConstructorArgs { get; set; }
    public List<InjectInfo> Injections { get; set; } = new();
    public List<SubscribeInfo> Subscriptions { get; set; } = new();
}

internal sealed class InjectInfo
{
    public string MemberName { get; set; } = "";
    public string TypeDisplay { get; set; } = "";
    public bool IsWritable { get; set; }
    public Location? Location { get; set; }
}

internal sealed class SubscribeInfo
{
    public string EventPath { get; set; } = "";
    public string HandlerName { get; set; } = "";
    public List<ISymbol>? EventPathMembers { get; set; }
    public Location? Location { get; set; }
}

internal sealed class PropertyBindingInfo
{
    public string FieldName { get; set; } = "";
    public string ControlType { get; set; } = "";
    public string ControlTypeFullName { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public string BindingMode { get; set; } = "DotPudica.Core.Binding.BindingMode.Default";
    public string? TargetProperty { get; set; }
    public string? SourceEvent { get; set; }
    public string? ConverterType { get; set; }
    public INamedTypeSymbol? ConverterSymbol { get; set; }
    public Location? Location { get; set; }
    public List<ISymbol>? PathMembers { get; set; }
    public string? FinalTypeDisplay { get; set; }
    public ITypeSymbol? SourceValueType { get; set; }
    public ITypeSymbol? TargetValueType { get; set; }
    public bool TargetPropertyWritable { get; set; }
    public string? BuiltInProxyTypeName { get; set; }
    public bool SkipGenerate { get; set; }
}

internal sealed class CommandBindingInfo
{
    public string FieldName { get; set; } = "";
    public string ControlType { get; set; } = "";
    public string CommandName { get; set; } = "";
    public string? ParameterPath { get; set; }
    public string Signal { get; set; } = "pressed";
    public Location? Location { get; set; }
    public List<ISymbol>? CommandPathMembers { get; set; }
    public string? CommandTypeDisplay { get; set; }
    public List<ISymbol>? ParameterPathMembers { get; set; }
}

internal sealed class CollectionBindingInfo
{
    public string FieldName { get; set; } = "";
    public string ControlType { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public string ItemScene { get; set; } = "";
    public int PoolSize { get; set; }
    public Location? Location { get; set; }
    public List<ISymbol>? PathMembers { get; set; }
    public string? CollectionTypeDisplay { get; set; }
    public ITypeSymbol? ElementTypeSymbol { get; set; }
    public string? ItemCommandPath { get; set; }
    public List<ISymbol>? ItemCommandPathMembers { get; set; }
    public ITypeSymbol? ItemCommandParameterType { get; set; }
    public bool IsVirtualized { get; set; }
}
