using Godot;

namespace DotPudica.Integration;

internal static class IntegrationTestHelpers
{
    public static async Task WaitFrames(Node host, int frames)
    {
        var tree = host.GetTree();
        for (var i = 0; i < frames; i++)
            await host.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    public static async Task WaitProcessFrame(Node host) => await WaitFrames(host, 1);
}
