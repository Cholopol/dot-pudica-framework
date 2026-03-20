using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotPudica.SourceGenerator;

/// <summary>
/// DotPudica binding source generator.
///
/// Working principle:
/// 1. Scan all partial classes marked with [DotPudicaView] or inheriting from one that is.
/// 2. Find [BindTo] / [BindCommand] fields.
/// 3. Inject MVVM runtime members and generated binding initialization code.
/// </summary>
[Generator]
internal sealed class BindingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var viewClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsRelevantClass,
                transform: TransformClassDeclaration)
            .Where(static info => info is not null)
            .Select(static (info, _) => info!);

        var collected = viewClasses.Collect();
        context.RegisterSourceOutput(collected, GenerateBindingCode);
    }

    private static bool IsRelevantClass(SyntaxNode node, CancellationToken ct)
    {
        if (node is not ClassDeclarationSyntax classSyntax)
            return false;

        if (!classSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            return false;

        foreach (var attrList in classSyntax.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var name = attr.Name.ToString();
                if (name is Constants.DotPudicaViewAttribute or "DotPudicaViewAttribute")
                    return true;
            }
        }

        foreach (var member in classSyntax.Members)
        {
            if (member is not FieldDeclarationSyntax field)
                continue;

            foreach (var attrList in field.AttributeLists)
            {
                foreach (var attr in attrList.Attributes)
                {
                    var name = attr.Name.ToString();
                    if (name is Constants.BindToAttribute or Constants.BindCommandAttribute
                        or "BindTo" or "BindCommand"
                        or "BindToAttribute" or "BindCommandAttribute")
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static ViewClassInfo? TransformClassDeclaration(
        GeneratorSyntaxContext ctx,
        CancellationToken ct)
    {
        var classSyntax = (ClassDeclarationSyntax)ctx.Node;
        var model = ctx.SemanticModel;

        if (model.GetDeclaredSymbol(classSyntax, ct) is not INamedTypeSymbol classSymbol)
            return null;

        var ownAttributeViewModelTypeName = GetOwnAttributeViewModelTypeName(classSymbol);
        var inheritedAttributeViewModelTypeName = ownAttributeViewModelTypeName is null
            ? GetInheritedAttributeViewModelTypeName(classSymbol.BaseType)
            : null;

        var viewModelTypeName = ownAttributeViewModelTypeName ?? inheritedAttributeViewModelTypeName;
        if (viewModelTypeName is null)
            return null;

        var info = new ViewClassInfo
        {
            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
            ClassName = classSymbol.Name,
            ViewModelTypeName = viewModelTypeName,
            HasReadyOverride = HasMethod(classSymbol, "_Ready"),
            HasExitTreeOverride = HasMethod(classSymbol, "_ExitTree"),
            OwnsDotPudicaRuntime = ownAttributeViewModelTypeName is not null,
        };

        foreach (var member in classSyntax.Members)
        {
            if (member is not FieldDeclarationSyntax fieldSyntax)
                continue;

            var typeInfo = model.GetTypeInfo(fieldSyntax.Declaration.Type, ct);
            var controlTypeName = typeInfo.Type?.Name ?? "";

            foreach (var variable in fieldSyntax.Declaration.Variables)
            {
                var fieldName = variable.Identifier.Text;

                var bindToAttr = GetAttribute(fieldSyntax, model, Constants.BindToAttributeFull, ct);
                if (bindToAttr is not null)
                {
                    var binding = ParseBindToAttribute(bindToAttr, fieldName, controlTypeName);
                    if (binding is not null)
                        info.PropertyBindings.Add(binding);
                }

                var bindCmdAttr = GetAttribute(fieldSyntax, model, Constants.BindCommandAttributeFull, ct);
                if (bindCmdAttr is not null)
                {
                    var binding = ParseBindCommandAttribute(bindCmdAttr, fieldName, controlTypeName);
                    if (binding is not null)
                        info.CommandBindings.Add(binding);
                }
            }
        }

        return info;
    }

    private static string? GetOwnAttributeViewModelTypeName(INamedTypeSymbol classSymbol)
    {
        foreach (var attr in classSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == Constants.DotPudicaViewAttributeFull
                && attr.ConstructorArguments.Length == 1
                && attr.ConstructorArguments[0].Value is INamedTypeSymbol viewModelType)
            {
                return viewModelType.ToDisplayString();
            }
        }

        return null;
    }

    private static string? GetInheritedAttributeViewModelTypeName(INamedTypeSymbol? baseType)
    {
        var currentSymbol = baseType;
        while (currentSymbol is not null)
        {
            var ownViewModelTypeName = GetOwnAttributeViewModelTypeName(currentSymbol);
            if (ownViewModelTypeName is not null)
                return ownViewModelTypeName;

            if (SymbolEqualityComparer.Default.Equals(currentSymbol, currentSymbol.BaseType))
                break;

            currentSymbol = currentSymbol.BaseType;
        }

        return null;
    }

    private static bool HasMethod(INamedTypeSymbol classSymbol, string methodName)
    {
        return classSymbol.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Any(m => !m.IsImplicitlyDeclared && m.Parameters.Length == 0);
    }

    private static AttributeData? GetAttribute(
        FieldDeclarationSyntax field,
        SemanticModel model,
        string fullName,
        CancellationToken ct)
    {
        foreach (var variable in field.Declaration.Variables)
        {
            if (model.GetDeclaredSymbol(variable, ct) is not IFieldSymbol fieldSymbol)
                continue;

            foreach (var attr in fieldSymbol.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == fullName)
                    return attr;
            }
        }

        return null;
    }

    private static PropertyBindingInfo? ParseBindToAttribute(
        AttributeData attr,
        string fieldName,
        string controlType)
    {
        if (attr.ConstructorArguments.Length == 0)
            return null;

        var path = attr.ConstructorArguments[0].Value?.ToString() ?? "";

        var binding = new PropertyBindingInfo
        {
            FieldName = fieldName,
            ControlType = controlType,
            SourcePath = path,
        };

        foreach (var namedArg in attr.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "Mode":
                    binding.BindingMode = GetEnumValueExpression(
                        namedArg.Value,
                        "DotPudica.Core.Binding.BindingMode.Default");
                    break;
                case "TargetProperty":
                    binding.TargetProperty = namedArg.Value.Value?.ToString();
                    break;
                case "SourceEvent":
                    binding.SourceEvent = namedArg.Value.Value?.ToString();
                    break;
                case "Converter":
                    if (namedArg.Value.Value is INamedTypeSymbol converterType)
                        binding.ConverterType = converterType.ToDisplayString();
                    break;
            }
        }

        if (Constants.ControlDefaults.TryGetValue(controlType, out var defaults))
        {
            binding.TargetProperty ??= defaults.Property;
            binding.SourceEvent ??= defaults.Signal;
        }

        if (binding.BindingMode == "DotPudica.Core.Binding.BindingMode.Default")
        {
            var hasTwoWaySignal = binding.SourceEvent is not null;
            binding.BindingMode = hasTwoWaySignal
                ? "DotPudica.Core.Binding.BindingMode.TwoWay"
                : "DotPudica.Core.Binding.BindingMode.OneWay";
        }

        return binding;
    }

    private static string GetEnumValueExpression(TypedConstant constant, string fallback)
    {
        if (constant.Kind != TypedConstantKind.Enum || constant.Type is not INamedTypeSymbol enumType)
            return fallback;

        foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>())
        {
            if (!member.HasConstantValue)
                continue;

            if (Equals(member.ConstantValue, constant.Value))
                return $"{enumType.ToDisplayString()}.{member.Name}";
        }

        return fallback;
    }

    private static CommandBindingInfo? ParseBindCommandAttribute(
        AttributeData attr,
        string fieldName,
        string controlType)
    {
        if (attr.ConstructorArguments.Length == 0)
            return null;

        var commandName = attr.ConstructorArguments[0].Value?.ToString() ?? "";

        var binding = new CommandBindingInfo
        {
            FieldName = fieldName,
            ControlType = controlType,
            CommandName = commandName,
            Signal = "pressed",
        };

        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == "ParameterPath")
                binding.ParameterPath = namedArg.Value.Value?.ToString();
        }

        return binding;
    }

    private static void GenerateBindingCode(
        SourceProductionContext ctx,
        System.Collections.Immutable.ImmutableArray<ViewClassInfo> views)
    {
        foreach (var view in views)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            var source = GenerateClassSource(view);
            ctx.AddSource($"{view.ClassName}.Bindings.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GenerateClassSource(ViewClassInfo view)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// This file is automatically generated by the DotPudica Source Generator. Do not modify it manually.");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using DotPudica.Core.Binding;");
        sb.AppendLine("using DotPudica.Godot.Views;");
        sb.AppendLine("using Godot;");
        sb.AppendLine();
        sb.Append("namespace ").Append(view.Namespace).AppendLine(";");
        sb.AppendLine();
        sb.Append("public partial class ").AppendLine(view.ClassName);
        sb.AppendLine("{");

        if (view.OwnsDotPudicaRuntime)
        {
            sb.Append("    protected readonly DotPudicaViewRuntime<")
                .Append(view.ViewModelTypeName)
                .AppendLine("> __dotPudicaView = new();");
            sb.AppendLine();
            sb.AppendLine("    public BindingContext BindingContext => __dotPudicaView.BindingContext;");
            sb.AppendLine();
            sb.Append("    public ")
                .Append(view.ViewModelTypeName)
                .AppendLine("? ViewModel");
            sb.AppendLine("    {");
            sb.AppendLine("        get => __dotPudicaView.ViewModel;");
            sb.AppendLine("        set => __dotPudicaView.ViewModel = value;");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    protected void DotPudicaInitialize()");
            sb.AppendLine("    {");
            sb.AppendLine("        __DotPudicaInitializeBindingsCore();");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    protected void DotPudicaDispose()");
            sb.AppendLine("    {");
            sb.AppendLine("        __dotPudicaView.Dispose();");
            sb.AppendLine("    }");
            sb.AppendLine();

            if (!view.HasReadyOverride)
            {
                sb.AppendLine("    public override void _Ready()");
                sb.AppendLine("    {");
                sb.AppendLine("        base._Ready();");
                sb.AppendLine("        DotPudicaInitialize();");
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            if (!view.HasExitTreeOverride)
            {
                sb.AppendLine("    public override void _ExitTree()");
                sb.AppendLine("    {");
                sb.AppendLine("        DotPudicaDispose();");
                sb.AppendLine("        base._ExitTree();");
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            sb.AppendLine("    protected virtual void __DotPudicaInitializeBindingsCore()");
            sb.AppendLine("    {");
            AppendBindingStatements(sb, view);
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        sb.AppendLine("    protected override void __DotPudicaInitializeBindingsCore()");
        sb.AppendLine("    {");
        sb.AppendLine("        base.__DotPudicaInitializeBindingsCore();");
        AppendBindingStatements(sb, view);
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void AppendBindingStatements(StringBuilder sb, ViewClassInfo view)
    {
        foreach (var b in view.PropertyBindings)
        {
            if (string.IsNullOrEmpty(b.TargetProperty))
                continue;

            var signalArg = b.SourceEvent is null ? "null" : $"\"{b.SourceEvent}\"";
            var converterArg = b.ConverterType is null ? "null" : $"new {b.ConverterType}()";

            sb.Append("        __dotPudicaView.BindProperty(");
            sb.Append(b.FieldName).Append(", ");
            sb.Append('"').Append(b.TargetProperty).Append("\", ");
            sb.Append(signalArg).Append(", ");
            sb.Append('"').Append(b.SourcePath).Append("\", ");
            sb.Append(b.BindingMode);
            if (b.ConverterType is not null)
                sb.Append(", ").Append(converterArg);
            sb.AppendLine(");");
        }

        foreach (var c in view.CommandBindings)
        {
            var paramArg = c.ParameterPath is null ? "null" : $"\"{c.ParameterPath}\"";

            sb.Append("        __dotPudicaView.BindCommand(");
            sb.Append(c.FieldName).Append(", ");
            sb.Append('"').Append(c.Signal).Append("\", ");
            sb.Append('"').Append(c.CommandName).Append("\", ");
            sb.Append(paramArg);
            sb.AppendLine(");");
        }
    }
}
