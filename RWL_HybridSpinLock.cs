class RWL_HybridSpinLock
{
    private volatile int readerCount;
    private SpinLock spinLock = new();

    public ReadLockHandle GetReadLock()
    {
        bool lockTaken = false;
        try
        {
            spinLock.Enter(ref lockTaken);
            Interlocked.Increment(ref readerCount);
        }
        finally
        {
            spinLock.Exit(false);
        }
        return new ReadLockHandle(this);
    }

    public WriteLockHandle GetShortReadLock()
    {
        bool lockTaken = false;
        spinLock.Enter(ref lockTaken);
        return new WriteLockHandle(this);
    }

    public WriteLockHandle GetWriteLock()
    {
        bool lockTaken = false;
        spinLock.Enter(ref lockTaken);
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

    public readonly struct ReadLockHandle(RWL_HybridSpinLock owner) : IDisposable
    {
        public readonly void Dispose()
            => Interlocked.Decrement(ref owner.readerCount);
    }

    public readonly struct WriteLockHandle(RWL_HybridSpinLock owner) : IDisposable
    {
        public readonly void Dispose()
            => owner.spinLock.Exit(false);
    }
}
