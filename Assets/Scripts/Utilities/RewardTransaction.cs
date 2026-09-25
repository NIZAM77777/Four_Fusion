// Handles reward-before-close, close-before-reward, failure and duplicate callbacks.
public sealed class RewardTransaction
{
    private bool earned, closed, failed, consumed;
    public void Earn() { earned = true; }
    public void Close(bool didFail) { closed = true; failed |= didFail; }
    public bool TryConsume()
    {
        if (!earned || !closed || failed || consumed) return false;
        consumed = true;
        return true;
    }
}
