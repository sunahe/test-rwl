class RWL_Spin
{
    private volatile int readerCount;

    public ReadLockHandle GetReadLock()
    {
        while (true)
        {
            var cnt = readerCount;
            if (cnt < int.MaxValue && Interlocked.CompareExchange(ref readerCount, cnt + 1, cnt) == cnt)
                return new ReadLockHandle(this);
        }
    }

    public ReadLockHandle GetShortReadLock() => GetReadLock();

    public WriteLockHandle GetWriteLock()
    {
        while (true)
        {
            if (Interlocked.CompareExchange(ref readerCount, int.MaxValue, 0) == 0)
                return new WriteLockHandle(this);
        }
    }

    public readonly struct ReadLockHandle(RWL_Spin owner) : IDisposable
    {
        public readonly void Dispose()
            => Interlocked.Decrement(ref owner.readerCount);
    }

    public readonly struct WriteLockHandle(RWL_Spin owner) : IDisposable
    {
        public readonly void Dispose()
        {
            owner.readerCount = 0;
        }
    }
}