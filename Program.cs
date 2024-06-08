Console.WriteLine(new TestState().Lock.GetType().Name);

void RwlReader(TestState state)
{
    while (state.cts.IsCancellationRequested is false)
    {
        using (state.Lock.GetReadLock())
        {
            if (state.FLAG)
            {
                Console.WriteLine("READER FOUND BUG");
                Environment.Exit(0);
            }

            int x = 0;
            for (int i = 0; i < 5_000; i++)
            {
                x += i % 973;
            }

            Interlocked.Increment(ref state.Ops);
        }
    }
}
void RwlWriter(TestState state)
{
    while (state.cts.IsCancellationRequested is false)
    {
        using (state.Lock.GetWriteLock())
        {
            if (state.FLAG)
            {
                Console.WriteLine("WRITER FOUND BUG");
                Environment.Exit(0);
            }
            state.FLAG = true;

            int x = 0;
            for (int i = 0; i < 5_000; i++)
            {
                x += i % 973;
            }

            Interlocked.Increment(ref state.Ops);
            state.FLAG = false;
        }
    }
}


long GetOps(Action<TestState> readAction, int readerCount, Action<TestState> writeAction, int writerCount)
{
    var state = new TestState();
    var readers = new Task[readerCount];
    for (int i = 0; i < readerCount; i++) readers[i] = Task.Run(() => readAction(state));
    var writers = new Task[writerCount];
    for (int i = 0; i < writerCount; i++) writers[i] = Task.Run(() => writeAction(state));
    Thread.Sleep(1);
    state.Ops = 0;
    Thread.Sleep(1000);
    state.cts.Cancel();
    Task.WaitAll(readers);
    Task.WaitAll(writers);
    return state.Ops;
}

var ops1 = GetOps(RwlReader, 1, RwlWriter, 0);
Console.WriteLine($"BASELINE: {ops1,20}");
double baseLine = ops1;
var ops2 = GetOps(RwlReader, 2, RwlWriter, 0);
Console.WriteLine($"2 READ: {ops2,22} / {ops2 / baseLine:G4}");
var ops2x = GetOps(RwlReader, 1, RwlWriter, 1);
Console.WriteLine($"1 READ 1 WRITE: {ops2x,14} / {ops2x / baseLine:G4}");
var ops4 = GetOps(RwlReader, 4, RwlWriter, 0);
Console.WriteLine($"4 READ: {ops4,22} / {ops4 / baseLine:G4}");
var ops4x = GetOps(RwlReader, 1, RwlWriter, 1);
Console.WriteLine($"3 READ 1 WRITE: {ops4x,14} / {ops4x / baseLine:G4}");
var ops8 = GetOps(RwlReader, 8, RwlWriter, 0);
Console.WriteLine($"8 READ: {ops8,22} / {ops8 / baseLine:G4}");
var ops8x = GetOps(RwlReader, 7, RwlWriter, 1);
Console.WriteLine($"7 READ 1 WRITE: {ops8x,14} / {ops8x / baseLine:G4}");
var ops8w = GetOps(RwlReader, 1, RwlWriter, 7);
Console.WriteLine($"1 READ 7 WRITE: {ops8w,14} / {ops8w / baseLine:G4}");
var ops8s = GetOps(RwlReader, 4, RwlWriter, 4);
Console.WriteLine($"4 READ 4 WRITE: {ops8s,14} / {ops8s / baseLine:G4}");


class TestState
{
    public bool FLAG;
    public long Ops;
    public RWL_Hybrid Lock = new();
    public CancellationTokenSource cts = new();
}

