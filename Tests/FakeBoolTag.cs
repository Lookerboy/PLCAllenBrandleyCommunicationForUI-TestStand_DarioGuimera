using PLC_COMM_API;

sealed class FakeBoolTag(BoolTagOptions options) : IBoolTag
{
    public readonly bool[] Plc = new bool[options.Length == 1 ? 8 : ((options.Length + 31) / 32) * 32];
    bool[] buffer = new bool[options.Length == 1 ? 8 : ((options.Length + 31) / 32) * 32];
    public readonly List<bool[]> Writes = new();
    public Func<Task>? BeforeRead;
    public Func<bool[], Task>? BeforeWrite;
    public int ReadFailures, WriteFailures, DisposeCount, Reads, InFlight, MaxInFlight;
    public bool ApplyBeforeWriteError;
    public int DelayMs;
    public async Task ReadAsync()
    {
        Enter();
        try
        {
            Reads++;
            if (BeforeRead != null) await BeforeRead();
            if (DelayMs > 0) await Task.Delay(DelayMs);
            if (ReadFailures > 0) { ReadFailures--; throw new IOException("Injected read timeout"); }
            buffer = Plc.ToArray();
        }
        finally { InFlight--; }
    }
    public async Task WriteAsync()
    {
        Enter();
        try
        {
            var values = buffer.ToArray();
            Writes.Add(values);
            if (BeforeWrite != null) await BeforeWrite(values);
            if (DelayMs > 0) await Task.Delay(DelayMs);
            if (ApplyBeforeWriteError || WriteFailures == 0) Array.Copy(values, Plc, Plc.Length);
            if (WriteFailures > 0) { WriteFailures--; throw new IOException("Injected write timeout"); }
        }
        finally { InFlight--; }
    }
    void Enter() { InFlight++; MaxInFlight = Math.Max(InFlight, MaxInFlight); if (InFlight > 1) throw new Exception("Overlapping native-tag operations"); }
    public bool GetBit(int index) => buffer[index];
    public void SetBit(int index, bool value) => buffer[index] = value;
    public void Dispose() { if (InFlight != 0) throw new Exception("Disposed an active tag"); DisposeCount++; }
}
