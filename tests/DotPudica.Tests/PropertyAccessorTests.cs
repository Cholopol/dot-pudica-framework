using DotPudica.Core.Binding;
using DotPudica.Tests.Fixtures;

namespace DotPudica.Tests;

/// <summary>
/// BindingPathFactory / TypedBindingPath delegate semantics tests.
/// Verifies the read/write behavior of factory-constructed paths (replaces the removed PropertyAccessor).
/// </summary>
public class PropertyAccessorTests
{
    [Fact]
    public void Get_SingleProperty_ReturnsValue()
    {
        var vm = new SimpleViewModel { Name = "Alice" };
        var path = BindingPathFactory.Create(
            static (SimpleViewModel x) => x.Name,
            static (x, v) => x.Name = v,
            "Name");
        path.Bind(vm);

        Assert.Equal("Alice", path.GetValue());
    }

    [Fact]
    public void Set_SingleProperty_UpdatesValue()
    {
        var vm = new SimpleViewModel { Name = "Old" };
        var path = BindingPathFactory.Create(
            static (SimpleViewModel x) => x.Name,
            static (x, v) => x.Name = v,
            "Name");
        path.Bind(vm);

        Assert.True(path.SetValue("New"));
        Assert.Equal("New", vm.Name);
    }

    [Fact]
    public void Get_NestedProperty_ReturnsDeepValue()
    {
        var vm = new NestedViewModel();
        vm.Model.Value = "Deep";
        var path = BindingPathFactory.CreateNested(
            static (NestedViewModel x) => x.Model.Value,
            static (x, v) => x.Model.Value = v,
            ["Model", "Value"],
            [static x => x.Model]);
        path.Bind(vm);

        Assert.Equal("Deep", path.GetValue());
    }

    [Fact]
    public void Set_NestedProperty_UpdatesDeepValue()
    {
        var vm = new NestedViewModel();
        var path = BindingPathFactory.CreateNested(
            static (NestedViewModel x) => x.Model.Value,
            static (x, v) => x.Model.Value = v,
            ["Model", "Value"],
            [static x => x.Model]);
        path.Bind(vm);

        Assert.True(path.SetValue("Updated"));
        Assert.Equal("Updated", vm.Model.Value);
    }

    [Fact]
    public void Set_OnReadOnlyProperty_ReturnsFalse()
    {
        var vm = new CommandViewModel();
        var path = BindingPathFactory.Create(
            static (CommandViewModel x) => x.DefaultCommand,
            setter: null,
            "DefaultCommand");
        path.Bind(vm);

        Assert.False(path.SetValue(vm.DefaultCommand));
    }

    [Fact]
    public void Create_MultiSegment_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            BindingPathFactory.Create(
                static (NestedViewModel x) => x.Model.Value,
                static (x, v) => x.Model.Value = v,
                "Model", "Value"));
    }

    [Fact]
    public void Get_DeepNestedPath_ReturnsValue()
    {
        var vm = new NestedViewModel();
        vm.Model.Child = new NestedChild { Text = "Leaf" };
        var path = BindingPathFactory.CreateNested(
            static (NestedViewModel x) => x.Model.Child!.Text,
            static (x, v) => x.Model.Child!.Text = v,
            ["Model", "Child", "Text"],
            [static x => x.Model, static x => x.Model.Child]);
        path.Bind(vm);

        Assert.Equal("Leaf", path.GetValue());
    }

    [Fact]
    public void Get_DeepNestedPath_NullIntermediate_ReturnsDefault()
    {
        var vm = new NestedViewModel();
        vm.Model.Child = null;
        var path = BindingPathFactory.CreateNested(
            static (NestedViewModel x) => x.Model.Child!.Text,
            static (x, v) => x.Model.Child!.Text = v,
            ["Model", "Child", "Text"],
            [static x => x.Model, static x => x.Model.Child]);
        path.Bind(vm);

        Assert.Null(path.GetValue());
    }

    [Fact]
    public void Get_Unbound_ReturnsDefault()
    {
        var path = BindingPathFactory.Create(
            static (SimpleViewModel x) => x.Name,
            static (x, v) => x.Name = v,
            "Name");

        Assert.Null(path.GetValue());
    }
}
