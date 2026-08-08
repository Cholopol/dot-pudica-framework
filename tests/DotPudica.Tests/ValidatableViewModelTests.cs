using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.ViewModels;

namespace DotPudica.Tests;

public class ValidatableViewModelTests
{
    [Fact]
    public void ValidateAll_Fails_WhenRequiredEmpty()
    {
        var vm = new SampleFormViewModel { Username = "", Email = "a@b.com" };

        Assert.False(vm.ValidateAll());
        Assert.True(vm.HasErrors);
    }

    [Fact]
    public void ValidateAll_Fails_WhenMinLengthNotMet()
    {
        var vm = new SampleFormViewModel { Username = "ab", Email = "a@b.com" };

        Assert.False(vm.ValidateAll());
        Assert.True(vm.HasErrors);
    }

    [Fact]
    public void ValidateAll_Succeeds_WhenConstraintsMet()
    {
        var vm = new SampleFormViewModel { Username = "alice", Email = "a@b.com" };

        Assert.True(vm.ValidateAll());
        Assert.False(vm.HasErrors);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var vm = new CountingDisposeViewModel();
        vm.Dispose();
        vm.Dispose();

        Assert.Equal(1, vm.OnDisposeCount);
    }
}

internal sealed class CountingDisposeViewModel : ValidatableViewModelBase
{
    public int OnDisposeCount { get; private set; }

    protected override void OnDispose() => OnDisposeCount++;
}

public partial class SampleFormViewModel : ValidatableViewModelBase
{
    [ObservableProperty]
    [Required(ErrorMessage = "username required")]
    [MinLength(3, ErrorMessage = "username too short")]
    private string _username = "";

    [ObservableProperty]
    [Required(ErrorMessage = "email required")]
    [EmailAddress(ErrorMessage = "email invalid")]
    private string _email = "";
}
