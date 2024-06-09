class RWL_ConditionalVariable
{
    private int readerCount;

    public ReadLockHandle GetReadLock()
    {
        lock (this)
        {
            while (readerCount == int.MaxValue)
            {
                Monitor.Wait(this);
            }
            readerCount++;
            return new ReadLockHandle(this);
        }
    }

    public ReadLockHandle GetShortReadLock() => GetReadLock();

    public WriteLockHandle GetWriteLock()
    {
        lock (this)
        {
            while (readerCount > 0)
            {
                Monitor.Wait(this);
            }
            readerCount = int.MaxValue;
            return new WriteLockHandle(this);
        }
    }

    public readonly struct ReadLockHandle(RWL_ConditionalVariable owner) : IDisposable
    {
        public readonly void Dispose()
        {
            lock (owner)
            {
                owner.readerCount--;
                Monitor.Pulse(owner);
            }
        }
    }

    public readonly struct WriteLockHandle(RWL_ConditionalVariable owner) : IDisposable
    {
        public readonly void Dispose()
        {
            lock (owner)
            {
                owner.readerCount = 0;
                Monitor.PulseAll(owner);
            }
        }
    }
}