using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotPudica.SourceGenerator;

[Generator]
public sealed class BindingGenerator : IIncrementalGenerator
{
    private const string ICommandFullName = "System.Windows.Input.ICommand";
    private const string INotifyCollectionChangedFullName =
        "System.Collections.Specialized.INotifyCollectionChanged";
    private const string ObservablePropertyAttributeFull =
        "CommunityToolkit.Mvvm.ComponentModel.ObservablePropertyAttribute";
    private const string RelayCommandAttributeFull =
        "CommunityToolkit.Mvvm.Input.RelayCommandAttribute";

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
            var attributeLists = member switch
            {
                FieldDeclarationSyntax field => field.AttributeLists,
                PropertyDeclarationSyntax property => property.AttributeLists,
                _ => default,
            };

            foreach (var attrList in attributeLists)
            {
                foreach (var attr in attrList.Attributes)
                {
                    var name = attr.Name.ToString();
                    if (name is Constants.BindToAttribute or Constants.BindCommandAttribute
                        or Constants.ItemsSourceAttribute
                        or "BindTo" or "BindCommand" or "ItemsSource"
                        or "BindToAttribute" or "BindCommandAttribute" or "ItemsSourceAttribute")
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

        var (ownViewModelSymbol, ownViewModelTypeName) = GetOwnAttributeViewModelType(classSymbol);
        INamedTypeSymbol? viewModelSymbol;
        string viewModelTypeName;

        if (ownViewModelSymbol is not null)
        {
            viewModelSymbol = ownViewModelSymbol;
            viewModelTypeName = ownViewModelTypeName!;
        }
        else
        {
            var inherited = GetInheritedAttributeViewModelType(classSymbol.BaseType);
            if (inherited is null)
                return null;
            viewModelSymbol = inherited.Value.Symbol;
            viewModelTypeName = inherited.Value.TypeName;
        }

        var info = new ViewClassInfo
        {
            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
            ClassName = classSymbol.Name,
            BaseTypeDisplay = classSymbol.BaseType?.ToDisplayString() ?? "",
            ViewModelTypeName = viewModelTypeName,
            ViewModelSymbol = viewModelSymbol,
            HasReadyOverride = HasMethod(classSymbol, "_Ready"),
            HasExitTreeOverride = HasMethod(classSymbol, "_ExitTree"),
            CallsInitializeView = CallsMethod(classSymbol, "_Ready", "InitializeView"),
            CallsDisposeView = CallsMethod(classSymbol, "_ExitTree", "DisposeView"),
            CallsRecycleView = CallsMethod(classSymbol, "_ExitTree", "RecycleView"),
            OwnsDotPudicaRuntime = ownViewModelSymbol is not null,
            Location = classSymbol.Locations.FirstOrDefault(),
        };

        if (info.OwnsDotPudicaRuntime)
        {
            ParseViewAttributeOptions(info, classSymbol);
            CollectInjections(info, classSyntax, model, ct);
            CollectFactoryMethod(info, classSymbol);
            CollectSubscriptions(info, classSymbol, viewModelSymbol);
            ResolveViewModelConstructor(info, viewModelSymbol);
        }

        foreach (var member in classSyntax.Members)
        {
            if (member is FieldDeclarationSyntax fieldSyntax)
            {
                var controlTypeSymbol = model.GetTypeInfo(fieldSyntax.Declaration.Type, ct).Type;
                foreach (var variable in fieldSyntax.Declaration.Variables)
                {
                    if (model.GetDeclaredSymbol(variable, ct) is IFieldSymbol fieldSymbol)
                        CollectBindings(info, fieldSymbol, variable.Identifier.Text, controlTypeSymbol, viewModelSymbol);
                }
                continue;
            }

            if (member is PropertyDeclarationSyntax propertySyntax
                && model.GetDeclaredSymbol(propertySyntax, ct) is IPropertySymbol propertySymbol)
            {
                var controlTypeSymbol = model.GetTypeInfo(propertySyntax.Type, ct).Type;
                CollectBindings(info, propertySymbol, propertySyntax.Identifier.Text, controlTypeSymbol, viewModelSymbol);
            }
        }

        return info;
    }

    private static void ParseViewAttributeOptions(ViewClassInfo info, INamedTypeSymbol classSymbol)
    {
        foreach (var attr in classSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != Constants.DotPudicaViewAttributeFull)
                continue;

            foreach (var namedArg in attr.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "Ownership":
                        info.OwnershipExpression = GetEnumValueExpression(
                            namedArg.Value,
                            "DotPudica.Core.ViewModels.ViewModelOwnership.Owned");
                        break;
                    case "AutoInitialize" when namedArg.Value.Value is bool auto:
                        info.AutoInitialize = auto;
                        break;
                    case "Pooled" when namedArg.Value.Value is bool pooled:
                        info.Pooled = pooled;
                        break;
                }
            }
        }
    }

    private static void CollectInjections(
        ViewClassInfo info,
        ClassDeclarationSyntax classSyntax,
        SemanticModel model,
        CancellationToken ct)
    {

        foreach (var member in classSyntax.Members)
        {
            if (member is FieldDeclarationSyntax fieldSyntax)
            {
                foreach (var variable in fieldSyntax.Declaration.Variables)
                {
                    if (model.GetDeclaredSymbol(variable, ct) is not IFieldSymbol fieldSymbol)
                        continue;
                    if (GetAttribute(fieldSymbol, Constants.InjectAttributeFull) is null)
                        continue;

                    info.Injections.Add(new InjectInfo
                    {
                        MemberName = variable.Identifier.Text,
                        TypeDisplay = fieldSymbol.Type.ToDisplayString(),
                        IsWritable = IsWritableMember(fieldSymbol),
                        Location = fieldSymbol.Locations.FirstOrDefault(),
                    });
                }
                continue;
            }

            if (member is PropertyDeclarationSyntax propertySyntax
                && model.GetDeclaredSymbol(propertySyntax, ct) is IPropertySymbol propertySymbol
                && GetAttribute(propertySymbol, Constants.InjectAttributeFull) is not null)
            {
                info.Injections.Add(new InjectInfo
                {
                    MemberName = propertySyntax.Identifier.Text,
                    TypeDisplay = propertySymbol.Type.ToDisplayString(),
                    IsWritable = IsWritableMember(propertySymbol),
                    Location = propertySymbol.Locations.FirstOrDefault(),
                });
            }
        }
    }

    private static void CollectFactoryMethod(ViewClassInfo info, INamedTypeSymbol classSymbol)
    {
        foreach (var method in classSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (GetAttribute(method, Constants.ViewModelFactoryAttributeFull) is null)
                continue;

            var isValid = !method.IsStatic
                && method.Parameters.Length == 0
                && method.ReturnsVoid is false
                && info.ViewModelSymbol is not null
                && IsAssignableTo(method.ReturnType, info.ViewModelSymbol);

            info.FactoryMethodName = method.Name;
            info.HasFactoryDeclaration = true;
            info.HasFactoryMethod = isValid;
            return;
        }
    }

    private static bool IsAssignableTo(ITypeSymbol source, ITypeSymbol target)
    {
        if (SymbolEqualityComparer.Default.Equals(source, target))
            return true;

        var current = source;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, target))
                return true;
            if (target.TypeKind == TypeKind.Interface
                && current.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, target)))
                return true;
            current = current.BaseType;
        }

        return false;
    }

    private static void CollectSubscriptions(
        ViewClassInfo info,
        INamedTypeSymbol classSymbol,
        INamedTypeSymbol? viewModelSymbol)
    {
        foreach (var method in classSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (method.IsStatic)
                continue;

            foreach (var attr in method.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() != Constants.SubscribeAttributeFull)
                    continue;
                if (attr.ConstructorArguments.Length != 1)
                    continue;

                var eventPath = attr.ConstructorArguments[0].Value?.ToString() ?? "";
                var subscription = new SubscribeInfo
                {
                    EventPath = eventPath,
                    HandlerName = method.Name,
                    Location = method.Locations.FirstOrDefault(),
                };

                if (viewModelSymbol is not null)
                    subscription.EventPathMembers = ResolveEventPath(viewModelSymbol, eventPath, method);

                info.Subscriptions.Add(subscription);
            }
        }
    }

    private static List<ISymbol>? ResolveEventPath(
        INamedTypeSymbol viewModelSymbol,
        string eventPath,
        IMethodSymbol handler)
    {
        var segments = eventPath.Split('.');
        var members = new List<ISymbol>();
        ITypeSymbol? currentType = viewModelSymbol;

        for (var i = 0; i < segments.Length; i++)
        {
            if (currentType is null)
                return null;

            var segment = segments[i];
            var isLast = i == segments.Length - 1;
            ISymbol? member = null;

            if (isLast)
            {
                member = currentType.GetMembers(segment)
                    .OfType<IEventSymbol>()
                    .FirstOrDefault() as ISymbol;
            }

            member ??= currentType.GetMembers(segment)
                .OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.GetMethod is not null) as ISymbol
                ?? currentType.GetMembers(segment)
                    .OfType<IFieldSymbol>()
                    .FirstOrDefault(f => !f.IsConst) as ISymbol;

            if (member is null)
                return null;

            members.Add(member);

            if (isLast)
            {
                if (member is not IEventSymbol eventSymbol)
                    return null;
                if (!IsCompatibleHandler(eventSymbol, handler))
                    return null;
                return members;
            }

            currentType = member switch
            {
                IPropertySymbol p => p.Type,
                IFieldSymbol f => f.Type,
                _ => null,
            };
        }

        return null;
    }

    private static bool IsCompatibleHandler(IEventSymbol eventSymbol, IMethodSymbol handler)
    {
        if ((eventSymbol.Type as INamedTypeSymbol)?.DelegateInvokeMethod is not { } invoke)
            return false;
        if (invoke.Parameters.Length != handler.Parameters.Length)
            return false;

        for (var i = 0; i < invoke.Parameters.Length; i++)
        {
            // Contravariance: the handler parameter must accept every value the event may deliver.
            if (!IsAssignableTo(invoke.Parameters[i].Type, handler.Parameters[i].Type))
                return false;
        }

        return invoke.ReturnsVoid == handler.ReturnsVoid;
    }

    private static void ResolveViewModelConstructor(ViewClassInfo info, INamedTypeSymbol? viewModelSymbol)
    {
        if (viewModelSymbol is null || info.HasFactoryMethod)
            return;

        if (viewModelSymbol.IsAbstract)
            return;

        var constructors = viewModelSymbol.InstanceConstructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        if (constructors.Count != 1)
            return;

        var args = new List<string>();
        foreach (var parameter in constructors[0].Parameters)
        {
            if (parameter.Type.TypeKind != TypeKind.Interface)
                return;

            args.Add($"__DotPudicaResolveService<{parameter.Type.ToDisplayString()}>()");
        }

        info.ViewModelConstructorArgs = string.Join(", ", args);
    }

    private static void CollectBindings(
        ViewClassInfo info,
        ISymbol targetMember,
        string targetMemberName,
        ITypeSymbol? controlTypeSymbol,
        INamedTypeSymbol? viewModelSymbol)
    {
        var controlTypeName = controlTypeSymbol?.Name ?? "";
        var controlTypeFullName = controlTypeSymbol?.ToDisplayString() ?? "";
        var location = targetMember.Locations.FirstOrDefault();

        var bindToAttr = GetAttribute(targetMember, Constants.BindToAttributeFull);
        if (bindToAttr is not null)
        {
            var binding = ParseBindToAttribute(bindToAttr, targetMemberName, controlTypeName, controlTypeFullName);
            if (binding is not null)
            {
                binding.Location = location;
                ResolvePropertyPath(binding, viewModelSymbol, controlTypeSymbol);
                info.PropertyBindings.Add(binding);
            }
        }

        var bindCmdAttr = GetAttribute(targetMember, Constants.BindCommandAttributeFull);
        if (bindCmdAttr is not null)
        {
            var binding = ParseBindCommandAttribute(bindCmdAttr, targetMemberName, controlTypeName);
            if (binding is not null)
            {
                binding.Location = location;
                ResolveCommandPath(binding, viewModelSymbol, controlTypeSymbol);
                info.CommandBindings.Add(binding);
            }
        }

        var itemsSourceAttr = GetAttribute(targetMember, Constants.ItemsSourceAttributeFull);
        if (itemsSourceAttr is not null)
        {
            var binding = ParseItemsSourceAttribute(itemsSourceAttr, targetMemberName, controlTypeName);
            if (binding is not null)
            {
                binding.Location = location;
                binding.IsVirtualized = IsVirtualizedItemsControl(controlTypeSymbol);
                ResolveCollectionPath(binding, viewModelSymbol);
                ResolveItemCommandPath(binding, viewModelSymbol);
                info.CollectionBindings.Add(binding);
            }
        }
    }

    private static bool IsVirtualizedItemsControl(ITypeSymbol? controlTypeSymbol)
    {
        var current = controlTypeSymbol;
        while (current is not null)
        {
            if (current.Name == Constants.VirtualizedItemsControlTypeName)
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static (INamedTypeSymbol? Symbol, string? TypeName) GetOwnAttributeViewModelType(
        INamedTypeSymbol classSymbol)
    {
        foreach (var attr in classSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == Constants.DotPudicaViewAttributeFull
                && attr.ConstructorArguments.Length == 1
                && attr.ConstructorArguments[0].Value is INamedTypeSymbol viewModelType)
            {
                return (viewModelType, viewModelType.ToDisplayString());
            }
        }
        return (null, null);
    }

    private static (INamedTypeSymbol Symbol, string TypeName)? GetInheritedAttributeViewModelType(
        INamedTypeSymbol? baseType)
    {
        var current = baseType;
        while (current is not null)
        {
            var (symbol, name) = GetOwnAttributeViewModelType(current);
            if (symbol is not null)
                return (symbol, name!);

            current = current.BaseType;
        }
        return null;
    }

    private static bool HasMethod(INamedTypeSymbol classSymbol, string methodName)
    {
        return classSymbol.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Any(m => !m.IsImplicitlyDeclared && m.Parameters.Length == 0);
    }

    // Godot only dispatches virtual overrides declared in user source; lifecycle hooks must call InitializeView/DisposeView/RecycleView from those overrides.
    private static bool CallsMethod(INamedTypeSymbol classSymbol, string methodName, string calledMethodName)
    {
        return classSymbol.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(m => !m.IsImplicitlyDeclared && m.Parameters.Length == 0)
            .SelectMany(m => m.DeclaringSyntaxReferences)
            .Select(reference => reference.GetSyntax())
            .OfType<MethodDeclarationSyntax>()
            .SelectMany(method => method.DescendantNodes().OfType<InvocationExpressionSyntax>())
            .Any(invocation => invocation.Expression switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText == calledMethodName,
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText == calledMethodName,
                _ => false,
            });
    }

    private static AttributeData? GetAttribute(ISymbol member, string fullName)
    {
        foreach (var attr in member.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == fullName)
                return attr;
        }
        return null;
    }

    private static PropertyBindingInfo? ParseBindToAttribute(
        AttributeData attr, string fieldName, string controlType, string controlTypeFullName)
    {
        if (attr.ConstructorArguments.Length == 0)
            return null;

        var path = attr.ConstructorArguments[0].Value?.ToString() ?? "";
        var binding = new PropertyBindingInfo
        {
            FieldName = fieldName,
            ControlType = controlType,
            ControlTypeFullName = controlTypeFullName,
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
                case "Target":
                    binding.TargetProperty = namedArg.Value.Value?.ToString();
                    break;
                case "Signal":
                    binding.SourceEvent = namedArg.Value.Value?.ToString();
                    break;
                case "Converter":
                    if (namedArg.Value.Value is INamedTypeSymbol converterType)
                    {
                        binding.ConverterType = converterType.ToDisplayString();
                        binding.ConverterSymbol = converterType;
                    }
                    break;
            }
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
        AttributeData attr, string fieldName, string controlType)
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
            switch (namedArg.Key)
            {
                case "Parameter":
                    binding.ParameterPath = namedArg.Value.Value?.ToString();
                    break;
                case "Signal":
                    binding.Signal = namedArg.Value.Value?.ToString() ?? "pressed";
                    break;
            }
        }

        return binding;
    }

    private static CollectionBindingInfo? ParseItemsSourceAttribute(
        AttributeData attr, string fieldName, string controlType)
    {
        if (attr.ConstructorArguments.Length < 2)
            return null;

        var path = attr.ConstructorArguments[0].Value?.ToString() ?? "";
        var itemScene = attr.ConstructorArguments[1].Value?.ToString() ?? "";

        var binding = new CollectionBindingInfo
        {
            FieldName = fieldName,
            ControlType = controlType,
            SourcePath = path,
            ItemScene = itemScene,
        };

        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == "PoolSize" && namedArg.Value.Value is int poolSize)
                binding.PoolSize = poolSize;
            else if (namedArg.Key == "ItemCommand" && namedArg.Value.Value is string itemCommand)
                binding.ItemCommandPath = itemCommand;
        }

        return binding;
    }

    private static void ResolvePropertyPath(
        PropertyBindingInfo binding,
        INamedTypeSymbol? viewModelSymbol,
        ITypeSymbol? controlTypeSymbol)
    {
        if (viewModelSymbol is null)
            return;

        var members = ResolvePath(viewModelSymbol, binding.SourcePath);
        if (members is null)
            return;

        binding.PathMembers = members;
        var finalMember = members[members.Count - 1];
        var finalType = GetMemberType(finalMember);
        if (finalType is not null)
        {
            binding.FinalTypeDisplay = finalType.ToDisplayString();
            binding.SourceValueType = finalType;
        }

        InferTargetAndSignal(binding, controlTypeSymbol);
        ResolveTargetPropertyType(binding, controlTypeSymbol);
        ResolveBuiltInProxy(binding, controlTypeSymbol);

        if (binding.BindingMode == "DotPudica.Core.Binding.BindingMode.Default")
        {
            var hasTwoWaySignal = binding.SourceEvent is not null;
            binding.BindingMode = hasTwoWaySignal
                ? "DotPudica.Core.Binding.BindingMode.TwoWay"
                : "DotPudica.Core.Binding.BindingMode.OneWay";
        }
    }

    private static void ResolveTargetPropertyType(
        PropertyBindingInfo binding,
        ITypeSymbol? controlTypeSymbol)
    {
        if (controlTypeSymbol is null || string.IsNullOrEmpty(binding.TargetProperty))
            return;

        var property = FindProperty(controlTypeSymbol, binding.TargetProperty!);
        if (property is null)
            return;

        binding.TargetValueType = property.Type;
        binding.TargetPropertyWritable = property.SetMethod is not null
            && property.SetMethod.DeclaredAccessibility == Accessibility.Public;
    }

    private static IPropertySymbol? FindProperty(ITypeSymbol type, string propertyName)
    {
        var current = type;
        while (current is not null)
        {
            var property = current.GetMembers(propertyName)
                .OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.GetMethod is not null);
            if (property is not null)
                return property;
            current = current.BaseType;
        }
        return null;
    }

    private static void ResolveBuiltInProxy(
        PropertyBindingInfo binding,
        ITypeSymbol? controlTypeSymbol)
    {
        if (controlTypeSymbol is null || string.IsNullOrEmpty(binding.TargetProperty))
            return;

        var current = controlTypeSymbol;
        while (current is not null)
        {
            if (Constants.BuiltInProxySupportedTargets.TryGetValue(current.Name, out var supportedTargets)
                && supportedTargets.Any(t => string.Equals(t, binding.TargetProperty, StringComparison.OrdinalIgnoreCase)))
            {
                binding.BuiltInProxyTypeName = Constants.BuiltInProxyTypes[current.Name];
                return;
            }
            current = current.BaseType;
        }
    }

    private static void ResolveCommandPath(
        CommandBindingInfo binding,
        INamedTypeSymbol? viewModelSymbol,
        ITypeSymbol? controlTypeSymbol)
    {
        if (viewModelSymbol is null)
            return;

        var commandMembers = ResolvePath(viewModelSymbol, binding.CommandName);
        if (commandMembers is null)
            return;

        var finalMember = commandMembers[commandMembers.Count - 1];

        // Special case for [RelayCommand] methods: the generated command type always implements ICommand
        if (finalMember is IMethodSymbol method
            && method.GetAttributes()
                .Any(a => a.AttributeClass?.ToDisplayString() == RelayCommandAttributeFull))
        {
            binding.CommandPathMembers = commandMembers;
            binding.CommandTypeDisplay = ICommandFullName;
            InferCommandSignal(binding, controlTypeSymbol);
            if (binding.ParameterPath is not null)
                binding.ParameterPathMembers = ResolvePath(viewModelSymbol, binding.ParameterPath);
            return;
        }

        var commandType = GetMemberType(finalMember);
        if (commandType is null)
            return;

        if (!ImplementsICommand(commandType))
            return;

        binding.CommandPathMembers = commandMembers;
        binding.CommandTypeDisplay = commandType.ToDisplayString();
        InferCommandSignal(binding, controlTypeSymbol);

        if (binding.ParameterPath is not null)
            binding.ParameterPathMembers = ResolvePath(viewModelSymbol, binding.ParameterPath);
    }

    private static void InferCommandSignal(CommandBindingInfo binding, ITypeSymbol? controlTypeSymbol)
    {
        if (controlTypeSymbol is not null
            && Constants.CommandSignals.TryGetValue(controlTypeSymbol.Name, out var signal))
        {
            binding.Signal = signal;
        }
    }

    private static void ResolveCollectionPath(
        CollectionBindingInfo binding,
        INamedTypeSymbol? viewModelSymbol)
    {
        if (viewModelSymbol is null)
            return;

        var members = ResolvePath(viewModelSymbol, binding.SourcePath);
        if (members is null)
            return;

        binding.PathMembers = members;
        var finalMember = members[members.Count - 1];
        var finalType = GetMemberType(finalMember);
        if (finalType is not null)
        {
            binding.CollectionTypeDisplay = finalType.ToDisplayString();
            binding.ElementTypeSymbol = GetEnumerableElementType(finalType);
        }
    }

    private static ITypeSymbol? GetEnumerableElementType(ITypeSymbol collectionType)
    {
        static bool IsGenericEnumerable(INamedTypeSymbol t) =>
            t is { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Collections_Generic_IEnumerable_T };

        if (collectionType is INamedTypeSymbol self && IsGenericEnumerable(self))
            return self.TypeArguments[0];

        foreach (var iface in collectionType.AllInterfaces)
        {
            if (IsGenericEnumerable(iface))
                return iface.TypeArguments[0];
        }

        return null;
    }

    private static void ResolveItemCommandPath(
        CollectionBindingInfo binding,
        INamedTypeSymbol? viewModelSymbol)
    {
        if (viewModelSymbol is null || string.IsNullOrEmpty(binding.ItemCommandPath))
            return;

        var members = ResolvePath(viewModelSymbol, binding.ItemCommandPath!);
        if (members is null)
            return;

        var finalMember = members[members.Count - 1];

        if (finalMember is IMethodSymbol method
            && method.GetAttributes()
                .Any(a => a.AttributeClass?.ToDisplayString() == RelayCommandAttributeFull))
        {
            binding.ItemCommandPathMembers = members;
            if (method.Parameters.Length == 1)
                binding.ItemCommandParameterType = method.Parameters[0].Type;
            return;
        }

        var commandType = GetMemberType(finalMember);
        if (commandType is null || !ImplementsICommand(commandType))
            return;

        binding.ItemCommandPathMembers = members;
    }

    private static List<ISymbol>? ResolvePath(INamedTypeSymbol sourceType, string path)
    {
        var segments = path.Split('.');
        var members = new List<ISymbol>();
        ITypeSymbol? currentType = sourceType;

        foreach (var segment in segments)
        {
            if (currentType is null)
                return null;

            ISymbol? member = currentType.GetMembers(segment)
                .OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.GetMethod is not null) as ISymbol
                ?? currentType.GetMembers(segment)
                    .OfType<IFieldSymbol>()
                    .FirstOrDefault(f => !f.IsConst) as ISymbol;

            // Cross-generator visibility fallback: CommunityToolkit.Mvvm [ObservableProperty]
            // Generated properties may not be visible in the current generator; inferred via field attributes.
            if (member is null)
                member = FindObservablePropertyMember(currentType, segment);

            // Cross-generator visibility fallback: CommunityToolkit.Mvvm [RelayCommand]
            // Generated command properties may not be visible in the current generator; inferred via method attributes.
            if (member is null)
                member = FindRelayCommandMember(currentType, segment);

            if (member is null)
                return null;

            members.Add(member);
            currentType = member is IMethodSymbol ? null : GetMemberType(member);
        }

        return members.Count == 0 ? null : members;
    }

    private static ISymbol? FindObservablePropertyMember(ITypeSymbol type, string propertyName)
    {
        foreach (var field in type.GetMembers().OfType<IFieldSymbol>())
        {
            if (field.IsConst)
                continue;

            var hasObservable = field.GetAttributes()
                .Any(a => a.AttributeClass?.ToDisplayString() == ObservablePropertyAttributeFull);
            if (!hasObservable)
                continue;

            if (GetGeneratedPropertyName(field.Name) == propertyName)
                return field;
        }
        return null;
    }

    private static string GetGeneratedPropertyName(string fieldName)
    {
        var name = fieldName;
        if (name.StartsWith("m_"))
            name = name.Substring(2);
        else if (name.StartsWith("s_"))
            name = name.Substring(2);
        else if (name.StartsWith("t_"))
            name = name.Substring(2);
        else if (name.StartsWith("_"))
            name = name.Substring(1);

        if (name.Length > 0)
            name = char.ToUpperInvariant(name[0]) + name.Substring(1);

        return name;
    }

    private static ISymbol? FindRelayCommandMember(ITypeSymbol type, string commandPropertyName)
    {
        foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
        {
            var hasRelay = method.GetAttributes()
                .Any(a => a.AttributeClass?.ToDisplayString() == RelayCommandAttributeFull);
            if (!hasRelay)
                continue;

            if (GetGeneratedCommandName(method.Name) == commandPropertyName)
                return method;
        }
        return null;
    }

    private static string GetGeneratedCommandName(string methodName)
    {
        var name = methodName;
        if (name.EndsWith("Async"))
            name = name.Substring(0, name.Length - 5);
        return name + "Command";
    }

    private static ITypeSymbol? GetMemberType(ISymbol member) => member switch
    {
        IPropertySymbol p => p.Type,
        IFieldSymbol f => f.Type,
        _ => null
    };

    private static bool ImplementsICommand(ITypeSymbol type)
    {
        if (type.ToDisplayString() == ICommandFullName)
            return true;

        var current = type;
        while (current is not null)
        {
            if (current is INamedTypeSymbol named
                && named.AllInterfaces.Any(i => i.ToDisplayString() == ICommandFullName))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static bool ImplementsINotifyCollectionChanged(ITypeSymbol type)
    {
        if (type.ToDisplayString() == INotifyCollectionChangedFullName)
            return true;

        var current = type;
        while (current is not null)
        {
            if (current is INamedTypeSymbol named
                && named.AllInterfaces.Any(i => i.ToDisplayString() == INotifyCollectionChangedFullName))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static void InferTargetAndSignal(
        PropertyBindingInfo binding, ITypeSymbol? controlTypeSymbol)
    {
        if (controlTypeSymbol is null)
            return;

        var current = controlTypeSymbol;
        while (current is not null)
        {
            if (Constants.ControlDefaults.TryGetValue(current.Name, out var defaults))
            {
                binding.TargetProperty ??= defaults.Property == "" ? null : defaults.Property;
                binding.SourceEvent ??= defaults.Signal;
                return;
            }
            current = current.BaseType;
        }
    }

    private static void GenerateBindingCode(
        SourceProductionContext ctx,
        System.Collections.Immutable.ImmutableArray<ViewClassInfo> views)
    {
        foreach (var view in views)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            ReportDiagnostics(ctx, view);

            var source = GenerateClassSource(view);
            ctx.AddSource($"{view.Namespace}.{view.ClassName}.Bindings.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static void ReportDiagnostics(SourceProductionContext ctx, ViewClassInfo view)
    {
        if (!view.OwnsDotPudicaRuntime)
            return;

        if (view.Pooled)
        {
            if (view.AutoInitialize)
            {
                if (view.HasFactoryDeclaration && !view.HasFactoryMethod)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ViewModelFactoryInvalid,
                        view.Location,
                        view.FactoryMethodName,
                        view.ViewModelTypeName));
                }
                else if (!view.HasFactoryDeclaration && view.ViewModelConstructorArgs is null)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ViewModelNotDiResolvable,
                        view.Location,
                        view.ViewModelTypeName));
                }
            }

            if (!view.HasReadyOverride || !view.CallsInitializeView)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.LifecycleEntryPointMissing,
                    view.Location,
                    view.ClassName,
                    "_Ready",
                    "InitializeView"));
            }

            if (!view.HasExitTreeOverride || !view.CallsRecycleView)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.LifecycleEntryPointMissing,
                    view.Location,
                    view.ClassName,
                    "_ExitTree",
                    "RecycleView"));
            }
        }
        else if (view.AutoInitialize)
        {
            if (!view.HasReadyOverride || !view.CallsInitializeView)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.LifecycleEntryPointMissing,
                    view.Location,
                    view.ClassName,
                    "_Ready",
                    "InitializeView"));
            }

            if (!view.HasExitTreeOverride || !view.CallsDisposeView)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.LifecycleEntryPointMissing,
                    view.Location,
                    view.ClassName,
                    "_ExitTree",
                    "DisposeView"));
            }

            if (view.HasFactoryDeclaration && !view.HasFactoryMethod)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ViewModelFactoryInvalid,
                    view.Location,
                    view.FactoryMethodName,
                    view.ViewModelTypeName));
            }
            else if (!view.HasFactoryDeclaration && view.ViewModelConstructorArgs is null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ViewModelNotDiResolvable,
                    view.Location,
                    view.ViewModelTypeName));
            }
        }
        else if (!view.HasExitTreeOverride || !view.CallsDisposeView)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LifecycleEntryPointMissing,
                view.Location,
                view.ClassName,
                "_ExitTree",
                "DisposeView"));
        }

        foreach (var injection in view.Injections)
        {
            if (!injection.IsWritable)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InjectNotWritable,
                    injection.Location,
                    injection.MemberName));
            }
        }

        foreach (var subscription in view.Subscriptions)
        {
            if (subscription.EventPathMembers is null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.SubscribeInvalid,
                    subscription.Location,
                    subscription.EventPath,
                    view.ViewModelTypeName,
                    subscription.HandlerName));
            }
        }

        foreach (var b in view.PropertyBindings)
        {
            if (b.PathMembers is null)
            {
                if (view.ViewModelSymbol is not null)
                    ReportPathNotFound(ctx, view, b.Location, b.SourcePath);
                b.SkipGenerate = true;
                continue;
            }

            if (HasStructIntermediateSegment(b.PathMembers, out var structSegment, out var structType))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.StructIntermediatePath,
                    b.Location,
                    b.SourcePath,
                    structSegment,
                    structType));
                b.SkipGenerate = true;
                continue;
            }

            if (string.IsNullOrEmpty(b.TargetProperty) || b.TargetValueType is null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.TargetPropertyInvalid,
                    b.Location,
                    b.ControlType,
                    b.TargetProperty ?? ""));
                b.SkipGenerate = true;
                continue;
            }

            if (!ValidatePropertyBindingTypes(b, out var typeDiag))
            {
                if (typeDiag is not null)
                    ctx.ReportDiagnostic(typeDiag);
                b.SkipGenerate = true;
            }
        }

        foreach (var c in view.CommandBindings)
        {
            if (c.CommandPathMembers is null)
            {
                if (view.ViewModelSymbol is not null)
                    ReportPathNotFound(ctx, view, c.Location, c.CommandName);
                continue;
            }

            // Special case for [RelayCommand] methods: type is already set to ICommand, skip ImplementsICommand check
            var finalMember = c.CommandPathMembers[c.CommandPathMembers.Count - 1];
            if (finalMember is IMethodSymbol)
                continue;

            var commandType = GetMemberType(finalMember);
            if (commandType is not null
                && !ImplementsICommand(commandType)
                && c.CommandTypeDisplay is not null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.CommandNotICommand,
                    c.Location,
                    c.CommandName,
                    finalMember.Name,
                    c.CommandTypeDisplay));
            }
        }

        foreach (var c in view.CollectionBindings)
        {
            if (c.PathMembers is null)
            {
                if (view.ViewModelSymbol is not null)
                    ReportPathNotFound(ctx, view, c.Location, c.SourcePath);
                continue;
            }

            if (c.IsVirtualized && c.PoolSize > 0)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.VirtualizedItemsPoolSize,
                    c.Location,
                    c.ControlType));
            }

            if (!IsObservableCollectionBinding(c)
                && c.CollectionTypeDisplay is not null)
            {
                var finalMember = c.PathMembers[c.PathMembers.Count - 1];
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.CollectionNotObservable,
                    c.Location,
                    c.SourcePath,
                    finalMember.Name,
                    c.CollectionTypeDisplay));
            }

            if (string.IsNullOrEmpty(c.ItemCommandPath))
                continue;

            if (c.ItemCommandPathMembers is null)
            {
                if (view.ViewModelSymbol is not null)
                    ReportPathNotFound(ctx, view, c.Location, c.ItemCommandPath!);
                continue;
            }

            if (c.ItemCommandParameterType is not null
                && c.ElementTypeSymbol is not null
                && !IsSameTypeIgnoringNrt(c.ItemCommandParameterType, c.ElementTypeSymbol))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ItemCommandParameterMismatch,
                    c.Location,
                    c.ItemCommandPath,
                    c.ItemCommandParameterType.ToDisplayString(),
                    c.SourcePath,
                    c.ElementTypeSymbol.ToDisplayString()));
                c.ItemCommandPathMembers = null;
            }
        }
    }

    private static void ReportPathNotFound(
        SourceProductionContext ctx,
        ViewClassInfo view,
        Location? location,
        string path)
    {
        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PathNotFound,
            location,
            view.ViewModelTypeName,
            path,
            path.Split('.')[0]));
    }

    private static bool HasStructIntermediateSegment(
        List<ISymbol> members,
        out string segmentName,
        out string typeName)
    {
        for (var i = 0; i < members.Count - 1; i++)
        {
            var type = GetMemberType(members[i]);
            if (type is null || type.IsReferenceType)
                continue;
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                continue;
            if (!type.IsValueType)
                continue;

            segmentName = GetAccessName(members[i]);
            typeName = type.ToDisplayString();
            return true;
        }

        segmentName = "";
        typeName = "";
        return false;
    }

    private static bool ValidatePropertyBindingTypes(
        PropertyBindingInfo binding,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        var sourceType = binding.SourceValueType;
        var targetType = binding.TargetValueType;
        if (sourceType is null || targetType is null)
            return false;

        if (binding.ConverterSymbol is not null)
        {
            if (ImplementsTypedConverter(binding.ConverterSymbol, sourceType, targetType))
                return true;

            diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ConverterNotTyped,
                binding.Location,
                binding.ConverterType,
                sourceType.ToDisplayString(),
                targetType.ToDisplayString());
            return false;
        }

        if (IsSameTypeIgnoringNrt(sourceType, targetType)
            || AreCompatibleNumericTypes(sourceType, targetType))
        {
            return true;
        }

        if (IsBoxingConversion(sourceType, targetType))
        {
            diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.BoxingConversionNotAllowed,
                binding.Location,
                binding.SourcePath,
                sourceType.ToDisplayString(),
                targetType.ToDisplayString());
            return false;
        }

        if (IsReferenceUpcast(sourceType, targetType))
        {
            if (IsTwoWayOrToSource(binding.BindingMode))
            {
                diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.TwoWayReferenceUpcastRequiresConverter,
                    binding.Location,
                    binding.SourcePath,
                    sourceType.ToDisplayString(),
                    targetType.ToDisplayString());
                return false;
            }

            return true;
        }

        diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.TypeMismatchWithoutConverter,
            binding.Location,
            binding.SourcePath,
            sourceType.ToDisplayString(),
            targetType.ToDisplayString());
        return false;
    }

    private static bool ImplementsTypedConverter(
        INamedTypeSymbol converterType,
        ITypeSymbol sourceType,
        ITypeSymbol targetType)
    {
        foreach (var iface in converterType.AllInterfaces)
        {
            if (iface is not { Name: "IValueConverter", TypeArguments.Length: 2 })
                continue;
            if (iface.ContainingNamespace?.ToDisplayString() != "DotPudica.Core.Binding")
                continue;
            if (IsSameTypeIgnoringNrt(iface.TypeArguments[0], sourceType)
                && IsSameTypeIgnoringNrt(iface.TypeArguments[1], targetType))
                return true;
        }
        return false;
    }

    private static bool IsSameTypeIgnoringNrt(ITypeSymbol source, ITypeSymbol target) =>
        SymbolEqualityComparer.Default.Equals(source, target);

    private static bool AreCompatibleNumericTypes(ITypeSymbol source, ITypeSymbol target)
    {
        static bool IsNumeric(SpecialType t) => t is
            SpecialType.System_Byte or SpecialType.System_SByte
            or SpecialType.System_Int16 or SpecialType.System_UInt16
            or SpecialType.System_Int32 or SpecialType.System_UInt32
            or SpecialType.System_Int64 or SpecialType.System_UInt64
            or SpecialType.System_Single or SpecialType.System_Double
            or SpecialType.System_Decimal;

        return IsNumeric(source.SpecialType) && IsNumeric(target.SpecialType);
    }

    private static bool IsBoxingConversion(ITypeSymbol source, ITypeSymbol target)
    {
        if (!source.IsValueType)
            return false;

        if (target.SpecialType == SpecialType.System_Object)
            return true;

        if (target.SpecialType == SpecialType.System_ValueType
            || target.SpecialType == SpecialType.System_Enum)
            return true;

        if (target.TypeKind == TypeKind.Interface)
            return true;

        return false;
    }

    private static bool IsReferenceUpcast(ITypeSymbol source, ITypeSymbol target)
    {
        if (source.IsValueType || target.IsValueType)
            return false;
        if (IsSameTypeIgnoringNrt(source, target))
            return false;

        if (target.TypeKind == TypeKind.Interface
            && source.AllInterfaces.Any(i => IsSameTypeIgnoringNrt(i, target)))
            return true;

        var current = source.BaseType;
        while (current is not null)
        {
            if (IsSameTypeIgnoringNrt(current, target))
                return true;
            current = current.BaseType;
        }

        return false;
    }

    private static bool IsTwoWayOrToSource(string bindingMode) =>
        bindingMode.IndexOf("TwoWay", StringComparison.Ordinal) >= 0
        || bindingMode.IndexOf("OneWayToSource", StringComparison.Ordinal) >= 0;

    private static string GenerateClassSource(ViewClassInfo view)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// This file is automatically generated by the DotPudica Source Generator. Do not modify it manually.");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Windows.Input;");
        sb.AppendLine("using DotPudica.Core.Binding;");
        sb.AppendLine("using DotPudica.Core.ViewModels;");
        sb.AppendLine("using DotPudica.Godot;");
        sb.AppendLine("using DotPudica.Godot.Binding;");
        sb.AppendLine("using DotPudica.Godot.Binding.ControlProxies;");
        sb.AppendLine("using DotPudica.Godot.Views;");
        sb.AppendLine("using Godot;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.Append("namespace ").Append(view.Namespace).AppendLine(";");
        sb.AppendLine();
        sb.Append("public partial class ").Append(view.ClassName);
        if (view.BaseTypeDisplay.Length > 0)
            sb.Append(" : global::").Append(view.BaseTypeDisplay);
        sb.AppendLine();
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
                .AppendLine("? ViewModel => __dotPudicaView.ViewModel;");
            sb.AppendLine();
            sb.Append("    protected void SetViewModel(")
                .Append(view.ViewModelTypeName)
                .AppendLine("? viewModel, ViewModelOwnership ownership)");
            sb.AppendLine("    {");
            sb.AppendLine("        __dotPudicaView.SetViewModel(viewModel, ownership);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.Append("    protected void BindVirtualizedItems<TCollection>(")
                .Append("DotPudica.Godot.Binding.ControlProxies.VirtualizedItemsControl target, ")
                .Append("string itemScenePath, TypedBindingPath<")
                .Append(view.ViewModelTypeName)
                .Append(", TCollection> sourcePath, Func<")
                .Append(view.ViewModelTypeName)
                .AppendLine(", ICommand>? itemCommandGetter = null)");
            sb.AppendLine("        where TCollection : class");
            sb.AppendLine("    {");
            sb.AppendLine("        __dotPudicaView.BindVirtualizedItems(target, itemScenePath, sourcePath, itemCommandGetter);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    protected void DotPudicaInitialize()");
            sb.AppendLine("    {");
            sb.AppendLine("        __dotPudicaView.CaptureUiContext();");
            sb.AppendLine("        __DotPudicaInitializeBindingsCore();");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    protected void DotPudicaDispose()");
            sb.AppendLine("    {");
            sb.AppendLine("        __dotPudicaView.Dispose();");
            sb.AppendLine("    }");
            sb.AppendLine();

            if (view.AutoInitialize)
            {
                AppendLifecycleMembers(sb, view);
                if (view.Pooled)
                    AppendRecycleViewMembers(sb, view);
            }
            else
            {
                sb.AppendLine("    partial void OnViewReady();");
                sb.AppendLine();
                sb.AppendLine("    partial void OnViewModelBound();");
                sb.AppendLine();
                sb.AppendLine("    partial void OnViewDisposing();");
                sb.AppendLine();

                if (view.Pooled)
                    AppendPooledViewMembers(sb, view);
                else
                    AppendInitializeViewOnly(sb, view);
            }

            AppendSharedBindingFields(sb, view);

            sb.AppendLine("    protected virtual void __DotPudicaInitializeBindingsCore()");
            sb.AppendLine("    {");
            AppendBindingStatements(sb, view);
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        AppendSharedBindingFields(sb, view);

        sb.AppendLine("    protected override void __DotPudicaInitializeBindingsCore()");
        sb.AppendLine("    {");
        sb.AppendLine("        base.__DotPudicaInitializeBindingsCore();");
        AppendBindingStatements(sb, view);
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void AppendInitializeViewCore(StringBuilder sb, ViewClassInfo view)
    {
        foreach (var injection in view.Injections)
        {
            if (!injection.IsWritable)
                continue;
            sb.Append("        ").Append(injection.MemberName).Append(" = __DotPudicaResolveService<")
                .Append(injection.TypeDisplay).AppendLine(">();");
        }

        sb.AppendLine("        OnViewReady();");
    }

    private static void AppendTeardownPrefix(StringBuilder sb, ViewClassInfo view)
    {
        sb.AppendLine("        OnViewDisposing();");
        AppendSubscribeStatements(sb, view, isSubscribe: false);
    }

    private static void AppendServiceResolverIfNeeded(StringBuilder sb, ViewClassInfo view)
    {
        if (!NeedsServiceResolver(view))
            return;

        sb.AppendLine("    private static T __DotPudicaResolveService<T>() where T : notnull");
        sb.AppendLine("    {");
        sb.AppendLine("        return DotPudica.Godot.AppContext.Current.Services.GetRequiredService<T>();");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void AppendLifecycleMembers(StringBuilder sb, ViewClassInfo view)
    {
        sb.AppendLine("    partial void OnViewReady();");
        sb.AppendLine();
        sb.AppendLine("    partial void OnViewModelBound();");
        sb.AppendLine();
        sb.AppendLine("    partial void OnViewDisposing();");
        sb.AppendLine();

        sb.AppendLine("    protected void InitializeView()");
        sb.AppendLine("    {");
        AppendInitializeViewCore(sb, view);
        sb.Append("        SetViewModel(CreateViewModel(), ").Append(view.OwnershipExpression).AppendLine(");");
        sb.AppendLine("        DotPudicaInitialize();");

        AppendSubscribeStatements(sb, view, isSubscribe: true);

        sb.AppendLine("        OnViewModelBound();");
        sb.AppendLine("    }");
        sb.AppendLine();

        AppendDisposeView(sb, view);

        sb.Append("    private ").Append(view.ViewModelTypeName).Append(" CreateViewModel()");
        if (view.HasFactoryDeclaration)
        {
            sb.Append(" => ").Append(view.FactoryMethodName).AppendLine("();");
        }
        else
        {
            sb.AppendLine(" =>");
            sb.Append("        new ").Append(view.ViewModelTypeName).Append('(')
                .Append(view.ViewModelConstructorArgs ?? "").AppendLine(");");
        }
        sb.AppendLine();

        AppendServiceResolverIfNeeded(sb, view);
    }

    private static void AppendInitializeViewOnly(StringBuilder sb, ViewClassInfo view)
    {
        sb.AppendLine("    protected void InitializeView()");
        sb.AppendLine("    {");
        AppendInitializeViewCore(sb, view);
        sb.AppendLine("    }");
        sb.AppendLine();

        AppendDisposeView(sb, view);

        AppendServiceResolverIfNeeded(sb, view);
    }

    private static void AppendPooledViewMembers(StringBuilder sb, ViewClassInfo view)
    {
        sb.AppendLine("    protected void InitializeView()");
        sb.AppendLine("    {");
        AppendInitializeViewCore(sb, view);
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.Append("    protected void ActivateViewModel(")
            .Append(view.ViewModelTypeName)
            .AppendLine(" viewModel)");
        sb.AppendLine("    {");
        sb.AppendLine("        SetViewModel(viewModel, DotPudica.Core.ViewModels.ViewModelOwnership.External);");
        sb.AppendLine("        DotPudicaInitialize();");

        AppendSubscribeStatements(sb, view, isSubscribe: true);

        sb.AppendLine("        OnViewModelBound();");
        sb.AppendLine("    }");
        sb.AppendLine();

        AppendRecycleViewMembers(sb, view);

        AppendDisposeView(sb, view);

        AppendServiceResolverIfNeeded(sb, view);
    }

    private static void AppendRecycleViewMembers(StringBuilder sb, ViewClassInfo view)
    {
        sb.AppendLine("    protected void RecycleView()");
        sb.AppendLine("    {");
        AppendTeardownPrefix(sb, view);
        sb.AppendLine("        __dotPudicaView.Recycle();");
        sb.AppendLine("        RequestReady();");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void AppendDisposeView(StringBuilder sb, ViewClassInfo view)
    {
        sb.AppendLine("    protected void DisposeView()");
        sb.AppendLine("    {");
        AppendTeardownPrefix(sb, view);
        sb.AppendLine("        DotPudicaDispose();");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static bool NeedsServiceResolver(ViewClassInfo view)
    {
        if (view.ViewModelConstructorArgs is not null
            && view.ViewModelConstructorArgs.Contains("__DotPudicaResolveService<"))
            return true;

        foreach (var injection in view.Injections)
        {
            if (injection.IsWritable)
                return true;
        }

        return false;
    }

    private static void AppendSubscribeStatements(StringBuilder sb, ViewClassInfo view, bool isSubscribe)
    {
        if (view.Subscriptions.Count == 0)
            return;

        var op = isSubscribe ? "+=" : "-=";
        sb.AppendLine("        if (ViewModel is { } __vm)");
        sb.AppendLine("        {");
        foreach (var subscription in view.Subscriptions)
        {
            if (subscription.EventPathMembers is null)
                continue;
            sb.Append("            __vm.")
                .Append(BuildMemberAccess(subscription.EventPathMembers))
                .Append(' ').Append(op).Append(' ')
                .Append(subscription.HandlerName).AppendLine(";");
        }
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static string BuildMemberAccess(List<ISymbol> members)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < members.Count; i++)
        {
            if (i > 0) sb.Append('.');
            sb.Append(GetAccessName(members[i]));
        }
        return sb.ToString();
    }

    private static void AppendSharedBindingFields(StringBuilder sb, ViewClassInfo view)
    {
        var emitted = new HashSet<string>(StringComparer.Ordinal);

        void EmitPathFields(string key, List<ISymbol> members, string valueTypeDisplay, bool canWrite)
        {
            if (!emitted.Add(key))
                return;

            var access = BuildPropertyAccess(members);
            sb.Append("    private static readonly Func<")
                .Append(view.ViewModelTypeName).Append(", ").Append(valueTypeDisplay)
                .Append("> ").Append(key).Append("_get = static vm => ")
                .Append(access).AppendLine(";");

            if (canWrite && members.Count == 1)
            {
                sb.Append("    private static readonly Action<")
                    .Append(view.ViewModelTypeName).Append(", ").Append(valueTypeDisplay)
                    .Append("> ").Append(key).Append("_set = static (vm, value) => ")
                    .Append(access).AppendLine(" = value;");
            }
            else if (canWrite)
            {
                sb.Append("    private static readonly Action<")
                    .Append(view.ViewModelTypeName).Append(", ").Append(valueTypeDisplay)
                    .Append("> ").Append(key).Append("_set = static (vm, value) => { ");
                AppendNestedSetterBody(sb, members);
                sb.AppendLine(" };");
            }

            sb.Append("    private static readonly string[] ").Append(key)
                .Append("_segments = new string[] { ");
            for (var i = 0; i < members.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append('"').Append(GetAccessName(members[i])).Append('"');
            }
            sb.AppendLine(" };");

            if (members.Count > 1)
            {
                sb.Append("    private static readonly Func<")
                    .Append(view.ViewModelTypeName)
                    .Append(", object?>[] ").Append(key)
                    .Append("_prefixes = new Func<")
                    .Append(view.ViewModelTypeName)
                    .Append(", object?>[] { ");
                for (var i = 0; i < members.Count - 1; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append("static vm => ");
                    sb.Append(BuildPrefixAccess(members, i + 1));
                }
                sb.AppendLine(" };");
            }
            else
            {
                sb.Append("    private static readonly Func<")
                    .Append(view.ViewModelTypeName)
                    .Append(", object?>[] ").Append(key)
                    .AppendLine("_prefixes = System.Array.Empty<System.Func<"
                        + view.ViewModelTypeName + ", object?>>();");
            }

            sb.AppendLine();
        }

        foreach (var b in view.PropertyBindings)
        {
            if (b.SkipGenerate || b.PathMembers is null || b.FinalTypeDisplay is null)
                continue;
            var key = SanitizeKey("prop_" + b.SourcePath);
            var canWrite = IsWritableMember(b.PathMembers[b.PathMembers.Count - 1]);
            EmitPathFields(key, b.PathMembers, b.FinalTypeDisplay, canWrite);
        }

        foreach (var c in view.CommandBindings)
        {
            if (c.CommandPathMembers is null)
                continue;
            var key = SanitizeKey("cmd_" + c.CommandName);
            EmitPathFields(key, c.CommandPathMembers, "ICommand", canWrite: false);

            if (c.ParameterPathMembers is not null)
            {
                var pkey = SanitizeKey("param_" + c.ParameterPath);
                var pType = GetMemberType(c.ParameterPathMembers[c.ParameterPathMembers.Count - 1])
                    ?.ToDisplayString() ?? "object?";
                EmitPathFields(pkey, c.ParameterPathMembers, pType, canWrite: false);
            }
        }

        foreach (var c in view.CollectionBindings)
        {
            if (c.PathMembers is null || !IsObservableCollectionBinding(c) || c.CollectionTypeDisplay is null)
                continue;
            var key = SanitizeKey("items_" + c.SourcePath);
            EmitPathFields(key, c.PathMembers, c.CollectionTypeDisplay, canWrite: false);

            if (c.ItemCommandPathMembers is null)
                continue;

            var itemCmdKey = SanitizeKey("itemcmd_" + c.ItemCommandPath);
            if (emitted.Add(itemCmdKey))
            {
                sb.Append("    private static readonly Func<")
                    .Append(view.ViewModelTypeName).Append(", ICommand> ")
                    .Append(itemCmdKey).Append("_get = static vm => ")
                    .Append(BuildPropertyAccess(c.ItemCommandPathMembers)).AppendLine(";");
                sb.AppendLine();
            }
        }
    }

    private static void AppendNestedSetterBody(StringBuilder sb, List<ISymbol> members)
    {
        for (var i = 0; i < members.Count - 1; i++)
        {
            if (i == 0)
                sb.Append("if (vm.").Append(GetAccessName(members[0])).Append(" is { } __n0");
            else
                sb.Append(" && __n").Append(i - 1).Append('.').Append(GetAccessName(members[i]))
                    .Append(" is { } __n").Append(i);
        }
        sb.Append(") __n").Append(members.Count - 2).Append('.')
            .Append(GetAccessName(members[members.Count - 1])).Append(" = value;");
    }

    private static string BuildPrefixAccess(List<ISymbol> members, int count)
    {
        var sb = new StringBuilder("vm");
        for (var i = 0; i < count; i++)
            sb.Append('.').Append(GetAccessName(members[i]));
        return sb.ToString();
    }

    private static string SanitizeKey(string raw)
    {
        var chars = raw.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]))
                chars[i] = '_';
        }
        return "__" + new string(chars);
    }

    private static bool IsWritableMember(ISymbol member) => member switch
    {
        IPropertySymbol p => p.SetMethod is not null,
        IFieldSymbol f => !f.IsReadOnly && !f.IsConst,
        _ => false
    };

    private static void AppendBindingStatements(StringBuilder sb, ViewClassInfo view)
    {
        foreach (var b in view.PropertyBindings)
        {
            if (b.SkipGenerate || b.PathMembers is null || string.IsNullOrEmpty(b.TargetProperty)
                || b.FinalTypeDisplay is null || b.TargetValueType is null)
                continue;

            var key = SanitizeKey("prop_" + b.SourcePath);
            var sourceType = b.FinalTypeDisplay;
            var targetType = b.TargetValueType.ToDisplayString();
            var canWrite = IsWritableMember(b.PathMembers[b.PathMembers.Count - 1]);
            var setterArg = canWrite ? $"{key}_set" : "null";

            sb.AppendLine("        {");
            sb.Append("            var __path = new TypedBindingPath<")
                .Append(view.ViewModelTypeName).Append(", ").Append(sourceType).Append(">(")
                .Append(key).Append("_get, ").Append(setterArg).Append(", ")
                .Append(key).Append("_segments, ").Append(key).AppendLine("_prefixes);");

            sb.Append("            var __proxy = ");
            AppendProxyConstruction(sb, b);
            sb.AppendLine(";");

            sb.Append("            __dotPudicaView.BindProperty<")
                .Append(sourceType).Append(", ").Append(targetType).Append(">(__proxy, __path, ")
                .Append(b.BindingMode);

            if (b.ConverterType is not null)
            {
                sb.Append(", converter: ");
                AppendConverterInstance(sb, b);
            }
            else if (!IsSameTypeIgnoringNrt(b.SourceValueType!, b.TargetValueType!))
            {
                if (AreCompatibleNumericTypes(b.SourceValueType!, b.TargetValueType!))
                {
                    sb.Append(", mapForward: static v => (").Append(targetType).Append(")v");
                    if (IsTwoWayOrToSource(b.BindingMode))
                        sb.Append(", mapBack: static v => (").Append(sourceType).Append(")v");
                }
                else if (IsReferenceUpcast(b.SourceValueType!, b.TargetValueType!))
                {
                    sb.Append(", mapForward: static v => (").Append(targetType).Append(")v");
                }
            }

            sb.AppendLine(");");
            sb.AppendLine("        }");
        }

        foreach (var c in view.CommandBindings)
        {
            if (c.CommandPathMembers is null)
                continue;

            var key = SanitizeKey("cmd_" + c.CommandName);
            sb.AppendLine("        {");
            sb.Append("            var __cmdPath = new TypedBindingPath<")
                .Append(view.ViewModelTypeName).Append(", ICommand>(")
                .Append(key).Append("_get, null, ")
                .Append(key).Append("_segments, ").Append(key).AppendLine("_prefixes);");

            if (c.ParameterPathMembers is not null)
            {
                var pkey = SanitizeKey("param_" + c.ParameterPath);
                var pType = GetMemberType(c.ParameterPathMembers[c.ParameterPathMembers.Count - 1])
                    ?.ToDisplayString() ?? "object?";
                sb.Append("            var __paramPath = new TypedBindingPath<")
                    .Append(view.ViewModelTypeName).Append(", object?>(")
                    .Append("static vm => (object?)").Append(pkey).Append("_get(vm), null, ")
                    .Append(pkey).Append("_segments, ").Append(pkey).AppendLine("_prefixes);");
                sb.Append("            __dotPudicaView.BindCommand(")
                    .Append(c.FieldName).Append(", \"")
                    .Append(c.Signal).Append("\", __cmdPath, __paramPath);");
            }
            else
            {
                sb.Append("            __dotPudicaView.BindCommand(")
                    .Append(c.FieldName).Append(", \"")
                    .Append(c.Signal).Append("\", __cmdPath);");
            }
            sb.AppendLine();
            sb.AppendLine("        }");
        }

        foreach (var c in view.CollectionBindings)
        {
            var pathMembers = c.PathMembers;
            if (pathMembers is null || !IsObservableCollectionBinding(c) || c.CollectionTypeDisplay is null)
                continue;

            var key = SanitizeKey("items_" + c.SourcePath);
            sb.AppendLine("        {");
            sb.Append("            var __itemsPath = new TypedBindingPath<")
                .Append(view.ViewModelTypeName).Append(", ").Append(c.CollectionTypeDisplay).Append(">(")
                .Append(key).Append("_get, null, ")
                .Append(key).Append("_segments, ").Append(key).AppendLine("_prefixes);");

            if (c.IsVirtualized)
            {
                sb.Append("            __dotPudicaView.BindVirtualizedItems(")
                    .Append(c.FieldName).Append(", \"")
                    .Append(c.ItemScene).Append("\", __itemsPath");
            }
            else
            {
                sb.Append("            __dotPudicaView.BindItems(")
                    .Append(c.FieldName).Append(", \"")
                    .Append(c.ItemScene).Append("\", __itemsPath, ")
                    .Append(c.PoolSize);
            }

            if (c.ItemCommandPathMembers is not null)
            {
                var itemCmdKey = SanitizeKey("itemcmd_" + c.ItemCommandPath);
                sb.Append(", ").Append(itemCmdKey).Append("_get");
            }

            sb.AppendLine(");");
            sb.AppendLine("        }");
        }
    }

    private static void AppendProxyConstruction(StringBuilder sb, PropertyBindingInfo binding)
    {
        if (binding.BuiltInProxyTypeName is not null)
        {
            if (binding.BuiltInProxyTypeName == "RichTextLabelProxy")
            {
                var useBbcode = binding.TargetProperty?.Equals("BbcodeText", StringComparison.OrdinalIgnoreCase) == true
                    ? "true" : "false";
                sb.Append("new RichTextLabelProxy(").Append(binding.FieldName)
                    .Append(", ").Append(useBbcode).Append(')');
            }
            else if (IsRangeBuiltInProxy(binding.BuiltInProxyTypeName))
            {
                sb.Append("new ").Append(binding.BuiltInProxyTypeName)
                    .Append('(').Append(binding.FieldName)
                    .Append(", DotPudica.Godot.Binding.RangeBindingProperty.")
                    .Append(ToRangeBindingPropertyName(binding.TargetProperty))
                    .Append(')');
            }
            else
            {
                sb.Append("new ").Append(binding.BuiltInProxyTypeName)
                    .Append('(').Append(binding.FieldName).Append(')');
            }
            return;
        }

        // DelegateTargetProxy for custom controls; Range subclass Min/Max/Value uses coordinated write.
        var controlType = binding.ControlTypeFullName;
        var targetType = binding.TargetValueType!.ToDisplayString();
        var signalArg = binding.SourceEvent is null ? "null" : $"\"{binding.SourceEvent}\"";
        sb.Append("new DelegateTargetProxy<").Append(controlType).Append(", ")
            .Append(targetType).Append(">(").Append(binding.FieldName);

        if (TryGetRangeBindingPropertyName(binding.TargetProperty, out var rangeProperty)
            && IsGodotRangeControlType(binding.ControlTypeFullName))
        {
            sb.Append(", static c => DotPudica.Godot.Binding.GodotRangeBinding.GetProperty(c, DotPudica.Godot.Binding.RangeBindingProperty.")
                .Append(rangeProperty).Append(')');
            if (binding.TargetPropertyWritable)
                sb.Append(", static (c, v) => DotPudica.Godot.Binding.GodotRangeBinding.SetProperty(c, DotPudica.Godot.Binding.RangeBindingProperty.")
                    .Append(rangeProperty).Append(", v)");
            else
                sb.Append(", null");
        }
        else
        {
            sb.Append(", static c => c.").Append(binding.TargetProperty);
            if (binding.TargetPropertyWritable)
                sb.Append(", static (c, v) => c.").Append(binding.TargetProperty).Append(" = v");
            else
                sb.Append(", null");
        }

        sb.Append(", ").Append(signalArg).Append(')');
    }

    private static bool IsRangeBuiltInProxy(string? proxyTypeName)
        => proxyTypeName is "ProgressBarProxy" or "SliderProxy" or "SpinBoxProxy";

    private static bool TryGetRangeBindingPropertyName(string? targetProperty, out string propertyName)
    {
        if (targetProperty is not { Length: > 0 } target)
        {
            propertyName = "";
            return false;
        }

        foreach (var name in Constants.RangeBindingTargetNames)
        {
            if (target.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                propertyName = name;
                return true;
            }
        }

        propertyName = "";
        return false;
    }

    private static string ToRangeBindingPropertyName(string? targetProperty)
        => TryGetRangeBindingPropertyName(targetProperty, out var name) ? name : "Value";

    private static bool IsGodotRangeControlType(string? controlTypeFullName)
    {
        if (controlTypeFullName is not { Length: > 0 } fullName)
            return false;

        foreach (var suffix in Constants.GodotRangeTypeNameSuffixes)
        {
            if (fullName.Equals("Godot." + suffix, StringComparison.Ordinal)
                || fullName.EndsWith("." + suffix, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static void AppendConverterInstance(StringBuilder sb, PropertyBindingInfo binding)
    {
        var converterType = binding.ConverterType!;
        if (binding.ConverterSymbol is not null
            && binding.ConverterSymbol.GetMembers("Instance")
                .OfType<IFieldSymbol>()
                .Any(f => f.IsStatic && f.IsReadOnly))
        {
            sb.Append(converterType).Append(".Instance");
            return;
        }

        sb.Append("new ").Append(converterType).Append("()");
    }

    private static bool IsObservableCollectionBinding(CollectionBindingInfo binding)
    {
        if (binding.PathMembers is not { Count: > 0 })
            return false;

        var finalMember = binding.PathMembers[binding.PathMembers.Count - 1];
        var collectionType = GetMemberType(finalMember);
        return collectionType is not null && ImplementsINotifyCollectionChanged(collectionType);
    }

    private static string BuildPropertyAccess(List<ISymbol> members)
    {
        var sb = new StringBuilder("vm");
        foreach (var member in members)
            sb.Append('.').Append(GetAccessName(member));
        return sb.ToString();
    }

    private static string GetAccessName(ISymbol member)
    {
        if (member is IFieldSymbol field
            && field.GetAttributes()
                .Any(a => a.AttributeClass?.ToDisplayString() == ObservablePropertyAttributeFull))
        {
            return GetGeneratedPropertyName(field.Name);
        }

        if (member is IMethodSymbol method
            && method.GetAttributes()
                .Any(a => a.AttributeClass?.ToDisplayString() == RelayCommandAttributeFull))
        {
            return GetGeneratedCommandName(method.Name);
        }

        return member.Name;
    }
}
