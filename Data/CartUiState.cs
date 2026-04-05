namespace The_Watch_Vault.Data;

/// <summary>
/// Scoped service that tracks cart open/close state and item count
/// across components within a single Blazor Server circuit.
/// </summary>
public sealed class CartUiState
{
    public event Action? Changed;

    public bool IsOpen { get; private set; }
    public int ItemCount { get; private set; }

    public void Open()
    {
        IsOpen = true;
        NotifyChanged();
    }

    public void Close()
    {
        IsOpen = false;
        NotifyChanged();
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;
        NotifyChanged();
    }

    public void SetItemCount(int count)
    {
        ItemCount = Math.Max(0, count);
        NotifyChanged();
    }

    public void NotifyChanged() => Changed?.Invoke();
}