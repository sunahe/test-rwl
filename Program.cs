Console.WriteLine($"=== {new TestState().Lock.GetType().Name} ===");

void DoWork(TestState state)
{
    int x = 0;
    for (int i = 0; i < 10; i++)
    {
        x += i % 973;
    }

    Interlocked.Increment(ref state.Ops);
}

void Worker(TestState state, double writePercentage)
{
    while (state.cts.IsCancellationRequested is false)
    {
        double x = Random.Shared.Next();
        if (x < writePercentage)
        {
            using (state.Lock.GetWriteLock())
            {
                if (state.FLAG) throw new Exception("WRITER BUG");
                state.FLAG = true;
                DoWork(state);
                state.FLAG = false;
            }
        }
        else
        {
            using (state.Lock.GetReadLock())
            {
                if (state.FLAG) throw new Exception("READER BUG");
                DoWork(state);
            }
        }
    }
}

long GetBaseline()
{
    var state = new TestState();
    var worker = Task.Run(() =>
    {
        while (state.cts.IsCancellationRequested is false)
        {
            _ = Random.Shared.Next();
            DoWork(state);
        }
    });
    Thread.Sleep(1);
    state.Ops = 0;
    Thread.Sleep(1000);
    state.cts.Cancel();
    worker.Wait();
    return state.Ops;
}

long GetOps(int workerCount, double writePercentage)
{
    var state = new TestState();
    var workers = new Task[workerCount];
    for (int i = 0; i < workerCount; i++)
        workers[i] = Task.Run(() => Worker(state, writePercentage));
    Thread.Sleep(1);
    state.Ops = 0;
    Thread.Sleep(1000);
    state.cts.Cancel();
    Task.WaitAll(workers);
    return state.Ops;
}

long ops = GetBaseline();
Console.WriteLine($"BASELINE: {ops,22} - {1_000_000.0 / ops:F2} us/op");
double baseLine = ops;
ops = GetOps(1, 0);
Console.WriteLine($"1 WORKER: {ops,22} / {ops / baseLine:F4}");
ops = GetOps(2, 0);
Console.WriteLine($"2 WORKERS: {ops,21} / {ops / baseLine:F4}");
ops = GetOps(2, 0.5);
Console.WriteLine($"2 WORKERS (50% writes): {ops,8} / {ops / baseLine:F4}");
ops = GetOps(4, 0);
Console.WriteLine($"4 WORKERS: {ops,21} / {ops / baseLine:F4}");
ops = GetOps(4, 0.5);
Console.WriteLine($"4 WORKERS (50% writes): {ops,8} / {ops / baseLine:F4}");
ops = GetOps(8, 0);
Console.WriteLine($"8 WORKERS: {ops,21} / {ops / baseLine:F4}");
ops = GetOps(8, 0.1);
Console.WriteLine($"8 WORKERS (10% writes): {ops,8} / {ops / baseLine:F4}");
ops = GetOps(8, 0.5);
Console.WriteLine($"8 WORKERS (50% writes): {ops,8} / {ops / baseLine:F4}");
ops = GetOps(8, 0.9);
Console.WriteLine($"8 WORKERS (90% writes): {ops,8} / {ops / baseLine:F4}");


class TestState
{
    public bool FLAG;
    public long Ops;
    public RWL_Hybrid Lock = new();
    public CancellationTokenSource cts = new();
}
