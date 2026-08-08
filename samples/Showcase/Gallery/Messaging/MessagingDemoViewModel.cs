using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.ViewModels;

namespace Samples.Showcase.Gallery.Messaging;

/// <summary>
/// Standalone recipient using ViewModelBase.Register.
/// Main Messaging page uses <see cref="MessagingPageViewModel"/>.
/// </summary>
public partial class MessagingDemoViewModel : ViewModelBase
{
    public MessagingDemoViewModel()
    {
        Register<PingMessage>((_, message) =>
            ReceivedText = $"#{message.Sequence} {message.Text}");
    }

    [ObservableProperty]
    private string _receivedText = "(none)";
}
