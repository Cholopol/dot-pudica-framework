using Godot;
using DotPudica.Core.Services;
using DotPudica.Core.Threading;

namespace DotPudica.Godot;

/// <summary>
/// Scene context host node: creates <see cref="ISceneScope"/> and <see cref="SceneOperationScope"/> on entering the tree,
/// disposes them after child Views are released when leaving the tree.
/// Godot guarantees child _ExitTree runs before parent _ExitTree, so teardown order stays correct.
/// Pair with <see cref="AppContext"/> (process-wide); this type is the per-scene context entry and must be attached manually.
/// </summary>
public partial class SceneContextHost : Node
{
    private ISceneScope? _scope;
    private SceneOperationScope? _operations;

    /// <summary>Current scene DI scope; only available after entering the scene tree.</summary>
    public ISceneScope Scope => _scope
        ?? throw new InvalidOperationException("SceneScope has not been created yet. Ensure the node has entered the scene tree.");

    /// <summary>Scene-level cancellation scope; cancelled on leaving room/disconnecting/exiting the tree.</summary>
    public SceneOperationScope Operations => _operations
        ?? throw new InvalidOperationException("SceneOperationScope has not been created yet.");

    public override void _EnterTree()
    {
        var root = AppContext.Current.Services;
        _scope = SceneScope.Create(root);
        _operations = new SceneOperationScope();
        base._EnterTree();
    }

    public override void _ExitTree()
    {
        _operations?.Dispose();
        _operations = null;
        _scope?.Dispose();
        _scope = null;
        base._ExitTree();
    }
}
