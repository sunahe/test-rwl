class RWL_DotNet
{
    private ReaderWriterLockSlim _lock = new();

    public ReadLockHandle GetReadLock()
    {
        _lock.EnterReadLock();
        return new ReadLockHandle(_lock);
    }

    public WriteLockHandle GetWriteLock()
    {
       _lock.EnterWriteLock();
        return new WriteLockHandle(_lock);
    }

    public readonly struct ReadLockHandle(ReaderWriterLockSlim _lock) : IDisposable
    {
        public readonly void Dispose()
            => _lock.ExitReadLock();
    }

    public readonly struct WriteLockHandle(ReaderWriterLockSlim _lock) : IDisposable
    {
        public readonly void Dispose()
            => _lock.ExitWriteLock();
    }
}