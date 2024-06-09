class RWL_SpinWait
{
    private volatile int readerCount;

    public ReadLockHandle GetReadLock()
    {
        while (true)
        {
            var spinWait = new SpinWait();
            var cnt = readerCount;
            if (cnt < int.MaxValue && Interlocked.CompareExchange(ref readerCount, cnt + 1, cnt) == cnt)
                return new ReadLockHandle(this);
            spinWait.SpinOnce(10);
        }
    }

    public WriteLockHandle GetWriteLock()
    {
        while (true)
        {
            var spinWait = new SpinWait();
            if (Interlocked.CompareExchange(ref readerCount, int.MaxValue, 0) == 0)
                return new WriteLockHandle(this);
            spinWait.SpinOnce(10);
        }
    }

    public readonly struct ReadLockHandle(RWL_SpinWait owner) : IDisposable
    {
        public readonly void Dispose()
            => Interlocked.Decrement(ref owner.readerCount);
    }

    public readonly struct WriteLockHandle(RWL_SpinWait owner) : IDisposable
    {
        public readonly void Dispose()
        {
            owner.readerCount = 0;
        }
    }
}