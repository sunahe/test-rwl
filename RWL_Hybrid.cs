class RWL_Hybrid
{
    private volatile int readerCount;

    public ReadLockHandle GetReadLock()
    {
        lock (this) // Wait for write lock to be released
        {
            Interlocked.Increment(ref readerCount);
        }
        return new ReadLockHandle(this);
    }

    public WriteLockHandle GetShortReadLock()
    {
        Monitor.Enter(this);
        return new WriteLockHandle(this);
    }

    public WriteLockHandle GetWriteLock()
    {
        Monitor.Enter(this);
        if (readerCount > 0)
        {
            SpinWait spinWait = new();
            do
            {
                spinWait.SpinOnce();
            } while (readerCount > 0);
        }

        return new WriteLockHandle(this);
    }

    public readonly struct ReadLockHandle(RWL_Hybrid owner) : IDisposable
    {
        public readonly void Dispose()
            => Interlocked.Decrement(ref owner.readerCount);
    }

    public readonly struct WriteLockHandle(RWL_Hybrid owner) : IDisposable
    {
        public readonly void Dispose()
            => Monitor.Exit(owner);
    }
}
