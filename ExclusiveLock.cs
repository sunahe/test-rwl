class ExclusiveLock
{
    public LockHandle GetReadLock() => GetWriteLock();

    public LockHandle GetShortReadLock() => GetWriteLock();

    public LockHandle GetWriteLock()
    {
        Monitor.Enter(this);
        return new LockHandle(this);
    }

    public readonly struct LockHandle(ExclusiveLock owner) : IDisposable
    {
        public readonly void Dispose()
            => Monitor.Exit(owner);
    }
}