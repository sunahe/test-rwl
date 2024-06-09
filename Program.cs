Console.WriteLine($"=== {new TestState().Lock.GetType().Name} ===");

void DoShortWork(TestState state)
{
    int x = 0;
    for (int i = 0; i < 10; i++)
    {
        x += i % 973;
    }

    Interlocked.Increment(ref state.Ops);
}

void DoWork(TestState state)
{
    int x = 0;
    for (int i = 0; i < 10_000; i++)
    {
        x += i % 973;
    }

    Interlocked.Increment(ref state.Ops);
}

void Worker(int workerIndex, TestState state, double writePercentage, double shortWorkPercentage)
{
    Random random = new Random(workerIndex);
    while (state.cts.IsCancellationRequested is false)
    {
        if (random.NextDouble() < writePercentage)
        {
            using (state.Lock.GetWriteLock())
            {
                if (state.FLAG) throw new Exception("WRITER BUG");
                state.FLAG = true;
                if (random.NextDouble() < shortWorkPercentage)
                    DoShortWork(state);
                else
                    DoWork(state);
                state.FLAG = false;
            }
        }
        else if (random.NextDouble() < shortWorkPercentage)
        {
            using (state.Lock.GetShortReadLock())
            {
                if (state.FLAG) throw new Exception("READER BUG");
                DoShortWork(state);
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

long GetBaseline(double shortWorkPercentage)
{
    var state = new TestState();
    var worker = Task.Run(() =>
    {
        Random random = new Random(0);
        while (state.cts.IsCancellationRequested is false)
        {
            _ = random.NextDouble();
            if (random.NextDouble() < shortWorkPercentage)
                DoShortWork(state);
            else
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

long GetOps(int workerCount, double writePercentage, double shortWorkPercentage)
{
    var state = new TestState();
    var workers = new Task[workerCount];
    for (int i = 0; i < workerCount; i++)
        workers[i] = Task.Run(() => Worker(i, state, writePercentage, shortWorkPercentage));
    Thread.Sleep(1);
    state.Ops = 0;
    Thread.Sleep(1000);
    state.cts.Cancel();
    Task.WaitAll(workers);
    return state.Ops;
}

void RunTests(double shortWorkPercentage)
{
    long ops = GetBaseline(shortWorkPercentage);
    Console.WriteLine($"BASELINE ({shortWorkPercentage * 100,3:G0}% sr): {ops,12} - {1_000_000.0 / ops:F2} us/op");
    double baseLine = ops;
    ops = GetOps(1, 0, shortWorkPercentage);
    Console.WriteLine($"1 WORKER: {ops,22} / {ops / baseLine:F4}");
    ops = GetOps(2, 0, shortWorkPercentage);
    Console.WriteLine($"2 WORKERS: {ops,21} / {ops / baseLine:F4}");
    ops = GetOps(2, 0.5, shortWorkPercentage);
    Console.WriteLine($"2 WORKERS (50% writes): {ops,8} / {ops / baseLine:F4}");
    ops = GetOps(4, 0, shortWorkPercentage);
    Console.WriteLine($"4 WORKERS: {ops,21} / {ops / baseLine:F4}");
    ops = GetOps(4, 0.5, shortWorkPercentage);
    Console.WriteLine($"4 WORKERS (50% writes): {ops,8} / {ops / baseLine:F4}");
    ops = GetOps(8, 0, shortWorkPercentage);
    Console.WriteLine($"8 WORKERS: {ops,21} / {ops / baseLine:F4}");
    ops = GetOps(8, 0.1, shortWorkPercentage);
    Console.WriteLine($"8 WORKERS (10% writes): {ops,8} / {ops / baseLine:F4}");
    ops = GetOps(8, 0.5, shortWorkPercentage);
    Console.WriteLine($"8 WORKERS (50% writes): {ops,8} / {ops / baseLine:F4}");
    ops = GetOps(8, 0.9, shortWorkPercentage);
    Console.WriteLine($"8 WORKERS (90% writes): {ops,8} / {ops / baseLine:F4}");
    Console.WriteLine();
}

RunTests(0);
RunTests(0.5);
RunTests(0.95);
RunTests(1);

class TestState
{
    public bool FLAG;
    public long Ops;
    public RWL_Hybrid Lock = new();
    public CancellationTokenSource cts = new();
}
