namespace DotPudica.Core.Threading;

/// <summary>Mutually exclusive async UI phases—avoids overlapping isLoading/hasError/isCancelled booleans.</summary>
public enum AsyncOperationState
{
    Idle,
    Running,
    Succeeded,
    Cancelled,
    Failed
}
