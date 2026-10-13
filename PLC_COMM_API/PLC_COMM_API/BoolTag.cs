using libplctag;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AllenBradley.OfflineTests")]

namespace PLC_COMM_API;

internal record BoolTagOptions(string Name, string Gateway, int Length, int TimeoutMs);

// Only the BOOL transport is substituted in tests; the public API remains unchanged.
internal interface IBoolTag : IDisposable
{
    Task ReadAsync();
    Task WriteAsync();
    bool GetBit(int index);
    void SetBit(int index, bool value);
}

internal sealed class NativeBoolTag : IBoolTag
{
    readonly Tag tag;
    public NativeBoolTag(BoolTagOptions options) => tag = new Tag
    {
        Name = options.Name, Gateway = options.Gateway, PlcType = PlcType.ControlLogix,
        Protocol = Protocol.ab_eip, Path = "1,0", UseConnectedMessaging = true,
        Timeout = TimeSpan.FromMilliseconds(options.TimeoutMs),
        ElementSize = options.Length == 1 ? 1 : 4,
        ElementCount = options.Length == 1 ? 1 : (options.Length + 31) / 32
    };
    public Task ReadAsync() => tag.ReadAsync();
    public Task WriteAsync() => tag.WriteAsync();
    public bool GetBit(int index) => tag.GetBit(index);
    public void SetBit(int index, bool value) => tag.SetBit(index, value);
    public void Dispose() => tag.Dispose();
}

internal sealed class BoolTagSession
{
    readonly IBoolTag tag;
    readonly SemaphoreSlim gate = new(1, 1);
    readonly string name;
    volatile bool closing;
    bool disposed;
    public int Length { get; }
    public BoolTagSession(BoolTagOptions options, IBoolTag tag)
        => (name, Length, this.tag) = (options.Name, options.Length, tag);

    public async Task<T> RunAsync<T>(string operationName, Func<IBoolTag, Task<T>> operation)
    {
        if (closing) throw new ObjectDisposedException(name);
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (closing) throw new ObjectDisposedException(name);
            return await operation(tag).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not ObjectDisposedException)
        { throw new IOException($"Fallo al {operationName} BOOL Allen Bradley en {name}: {ex.Message}", ex); }
        finally { gate.Release(); }
    }

    // Preserve bounded read retries. Writes are awaited once: an uncertain write
    // is reported, never replayed automatically after the operator has released.
    public static async Task ReadAsync(IBoolTag tag)
    {
        for (int attempt = 0; ; attempt++)
        {
            try { await tag.ReadAsync().ConfigureAwait(false); return; }
            catch when (attempt < 5) { await Task.Delay(100).ConfigureAwait(false); }
        }
    }

    public void Close()
    {
        closing = true;
        gate.Wait();
        try { if (!disposed) { disposed = true; tag.Dispose(); } }
        finally { gate.Release(); }
    }
}
