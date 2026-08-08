using DotPudica.Core.Binding;
using DotPudica.Tests.Fixtures;

namespace DotPudica.Tests;

/// <summary>
/// TypedBindingPath unit tests. Verifies chained listening and value read/write.
/// </summary>
public class TypedBindingPathTests
{
    private static TypedBindingPath<SimpleViewModel, string> NamePath()
        => BindingPathFactory.Create(
            static (SimpleViewModel vm) => vm.Name,
            static (vm, v) => vm.Name = v,
            "Name");

    private static TypedBindingPath<NestedViewModel, string> ModelValuePath()
        => BindingPathFactory.CreateNested(
            static (NestedViewModel vm) => vm.Model.Value,
            static (vm, v) => vm.Model.Value = v,
            ["Model", "Value"],
            [static vm => vm.Model]);

    private static TypedBindingPath<NestedViewModel, string> ModelChildTextPath()
        => BindingPathFactory.CreateNested(
            static (NestedViewModel vm) => vm.Model.Child!.Text,
            static (vm, v) => vm.Model.Child!.Text = v,
            ["Model", "Child", "Text"],
            [static vm => vm.Model, static vm => vm.Model.Child]);

    [Fact]
    public void GetValue_AfterBind_ReturnsCurrentValue()
    {
        var vm = new SimpleViewModel { Name = "Init" };
        var path = NamePath();

        path.Bind(vm);

        Assert.Equal("Init", path.GetValue());
    }

    [Fact]
    public void ValueChanged_SingleProperty_TriggersOnPropertyChange()
    {
        var vm = new SimpleViewModel { Name = "Old" };
        var path = NamePath();
        path.Bind(vm);

        var triggered = false;
        path.ValueChanged += (_, _) => triggered = true;

        vm.Name = "New";

        Assert.True(triggered);
        Assert.Equal("New", path.GetValue());
    }

    [Fact]
    public void ValueChanged_NestedProperty_TriggersOnLeafChange()
    {
        var vm = new NestedViewModel();
        vm.Model.Value = "Old";
        var path = ModelValuePath();
        path.Bind(vm);

        var triggered = false;
        path.ValueChanged += (_, _) => triggered = true;

        vm.Model.Value = "New";

        Assert.True(triggered);
        Assert.Equal("New", path.GetValue());
    }

    [Fact]
    public void ValueChanged_NestedProperty_TriggersOnIntermediateChange()
    {
        var vm = new NestedViewModel();
        vm.Model = new NestedModel { Value = "First" };
        var path = ModelValuePath();
        path.Bind(vm);

        var triggered = false;
        path.ValueChanged += (_, _) => triggered = true;

        vm.Model = new NestedModel { Value = "Second" };

        Assert.True(triggered);
        Assert.Equal("Second", path.GetValue());
    }

    [Fact]
    public void ValueChanged_DeepNestedPath_TriggersOnLeafChange()
    {
        var vm = new NestedViewModel();
        vm.Model.Child = new NestedChild { Text = "Old" };
        var path = ModelChildTextPath();
        path.Bind(vm);

        var triggered = false;
        path.ValueChanged += (_, _) => triggered = true;

        vm.Model.Child!.Text = "New";

        Assert.True(triggered);
        Assert.Equal("New", path.GetValue());
    }

    [Fact]
    public void ValueChanged_DeepNestedPath_TriggersOnIntermediateChange()
    {
        var vm = new NestedViewModel();
        vm.Model.Child = new NestedChild { Text = "Old" };
        var path = ModelChildTextPath();
        path.Bind(vm);

        var triggered = false;
        path.ValueChanged += (_, _) => triggered = true;

        vm.Model.Child = new NestedChild { Text = "New" };

        Assert.True(triggered);
        Assert.Equal("New", path.GetValue());
    }

    [Fact]
    public void Unbind_StopsReceivingNotifications()
    {
        var vm = new SimpleViewModel { Name = "Old" };
        var path = NamePath();
        path.Bind(vm);

        var triggered = false;
        path.ValueChanged += (_, _) => triggered = true;

        path.Unbind();
        vm.Name = "New";

        Assert.False(triggered);
    }

    [Fact]
    public void SetValue_UpdatesProperty()
    {
        var vm = new SimpleViewModel { Name = "Old" };
        var path = NamePath();
        path.Bind(vm);

        var result = path.SetValue("New");

        Assert.True(result);
        Assert.Equal("New", vm.Name);
    }

    [Fact]
    public void Bind_NullSource_DoesNotThrow()
    {
        var path = NamePath();

        path.Bind(null);

        Assert.Null(path.GetValue());
    }

    [Fact]
    public void Bind_ReplacesPreviousSource()
    {
        var vm1 = new SimpleViewModel { Name = "First" };
        var vm2 = new SimpleViewModel { Name = "Second" };
        var path = NamePath();

        path.Bind(vm1);
        path.Bind(vm2);

        Assert.Equal("Second", path.GetValue());
    }

    [Fact]
    public void SetValue_ReadOnlyPath_ReturnsFalse()
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
    public void GetValue_DeepNestedPath_NullIntermediate_ReturnsDefault()
    {
        var vm = new NestedViewModel();
        vm.Model.Child = null;
        var path = ModelChildTextPath();
        path.Bind(vm);

        Assert.Null(path.GetValue());
    }

    [Fact]
    public void Dispose_CleansUpListeners()
    {
        var vm = new SimpleViewModel { Name = "Old" };
        var path = NamePath();
        var changed = 0;
        path.ValueChanged += (_, _) => changed++;
        path.Bind(vm);

        path.Dispose();

        vm.Name = "New";
        Assert.Equal(0, changed);
    }
}
