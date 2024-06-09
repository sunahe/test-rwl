class ExclusiveSpinLock
{
    private SpinLock spinLock = new();

    public LockHandle GetReadLock() => GetWriteLock();

    public LockHandle GetShortReadLock() => GetWriteLock();

    public LockHandle GetWriteLock()
    {
        bool lockTaken = false;
        spinLock.Enter(ref lockTaken);
        return new LockHandle(this);
    }

    public readonly struct LockHandle(ExclusiveSpinLock owner) : IDisposable
    {
        public readonly void Dispose()
            => owner.spinLock.Exit(false);
    }
}