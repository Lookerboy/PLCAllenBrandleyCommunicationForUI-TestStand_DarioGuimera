namespace libplctag.DataTypes { }
namespace libplctag
{
    public enum PlcType { ControlLogix }
    public enum Protocol { ab_eip }
    public partial class Tag : IDisposable
    {
        public static int Writes;
        public static bool PlcValue;
        public static Task? WriteGate;
        public static Func<bool, Task>? BeforeWrite;
        bool[] buffer = new bool[256];
        public string Name { get; set; } = "";
        public string Gateway { get; set; } = "";
        public string Path { get; set; } = "";
        public PlcType PlcType { get; set; }
        public Protocol Protocol { get; set; }
        public TimeSpan Timeout { get; set; }
        public int ElementSize { get; set; }
        public int ElementCount { get; set; }
        public bool UseConnectedMessaging { get; set; }
        public Task ReadAsync() { buffer[0] = PlcValue; return Task.CompletedTask; }
        public async Task WriteAsync()
        {
            bool value = buffer[0];
            if (WriteGate != null) await WriteGate;
            if (BeforeWrite != null) await BeforeWrite(value);
            PlcValue = value;
            Writes++;
        }
        public bool GetBit(int bit) => buffer[bit];
        public void SetBit(int bit, bool value) => buffer[bit] = value;
        public void Dispose() { Writes = 0; PlcValue = false; }
    }
}
