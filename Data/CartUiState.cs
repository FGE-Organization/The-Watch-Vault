namespace The_Watch_Vault.Data;

public sealed class CartUiState
{
    public event Action? Changed;

    public bool IsOpen { get; private set; }

    public void Open()
    {
        if (IsOpen)
        {
            return;
        }

        IsOpen = true;
        Changed?.Invoke();
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        Changed?.Invoke();
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;
        Changed?.Invoke();
    }

    public void NotifyChanged() => Changed?.Invoke();
}