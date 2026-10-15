namespace PLC_COMM_API;

public partial class API_UI
{
    readonly object boolRegistryLock = new();
    readonly Dictionary<string, BoolTagSession> boolTags = new(StringComparer.OrdinalIgnoreCase);
    readonly Func<BoolTagOptions, IBoolTag> boolTagFactory = options => new NativeBoolTag(options);
    bool boolClosed;

    internal API_UI(string gateway, Func<BoolTagOptions, IBoolTag> factory) : this(gateway)
        => boolTagFactory = factory;

    void AddBoolVariable(string name, int length, int timeout)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (length < 1 || timeout < 1) throw new ArgumentOutOfRangeException(nameof(length));
        lock (boolRegistryLock)
        {
            if (boolClosed) throw new ObjectDisposedException(nameof(API_UI));
            if (boolTags.TryGetValue(name, out var existing))
            {
                if (existing.Length != length) throw new ArgumentException($"Longitudes BOOL incompatibles para {name}.");
                return; // One buffer/operation gate per symbol, even with several controls.
            }
            var options = new BoolTagOptions(name, APIGateWay, length, timeout);
            boolTags.Add(name, new BoolTagSession(options, boolTagFactory(options)));
        }
    }

    BoolTagSession BoolSession(string name)
    {
        lock (boolRegistryLock)
        {
            if (boolClosed) throw new ObjectDisposedException(nameof(API_UI));
            return boolTags.TryGetValue(name, out var value) ? value
                : throw new ArgumentException($"Variable BOOL no registrada: {name}.");
        }
    }

    void CloseBoolVariables()
    {
        BoolTagSession[] sessions;
        lock (boolRegistryLock) { boolClosed = true; sessions = boolTags.Values.ToArray(); }
        var errors = new List<Exception>();
        foreach (var session in sessions) { try { session.Close(); } catch (Exception ex) { errors.Add(ex); } }
        if (errors.Count > 0) throw new AggregateException("Error al cerrar tags BOOL.", errors);
    }

    public Task<bool> GetVariableBool(string VariableName) => GetIndexArrayBool(VariableName, 0);

    public Task<bool> GetIndexArrayBool(string VariableName, int Index)
    {
        var session = BoolSession(VariableName);
        ValidateBoolIndex(session, Index);
        return session.RunAsync("leer", async tag =>
        {
            await BoolTagSession.ReadAsync(tag).ConfigureAwait(false);
            return tag.GetBit(Index);
        });
    }

    public Task<List<bool>> GetVariableArrayBool(string VariableName)
    {
        var session = BoolSession(VariableName);
        return session.RunAsync("leer", async tag =>
        {
            await BoolTagSession.ReadAsync(tag).ConfigureAwait(false);
            return Enumerable.Range(0, session.Length).Select(tag.GetBit).ToList();
        });
    }

    public Task SetVariableBool(string VariableName, bool VariableValue)
        => SetIndexArrayBool(VariableName, 0, VariableValue);

    public Task SetIndexArrayBool(string VariableName, int Index, bool VariableValue)
    {
        var session = BoolSession(VariableName);
        ValidateBoolIndex(session, Index);
        return session.RunAsync("escribir", async tag =>
        {
            // Read, modify only the requested bit, and write under the same gate.
            await BoolTagSession.ReadAsync(tag).ConfigureAwait(false);
            tag.SetBit(Index, VariableValue);
            await tag.WriteAsync().ConfigureAwait(false);
            return true;
        });
    }

    public Task SetVariableArrayBool(string VariableName, List<bool> VariableValues)
    {
        var session = BoolSession(VariableName);
        ArgumentNullException.ThrowIfNull(VariableValues);
        if (VariableValues.Count > session.Length) throw new ArgumentOutOfRangeException(nameof(VariableValues));
        var values = VariableValues.ToArray();
        return session.RunAsync("escribir", async tag =>
        {
            await BoolTagSession.ReadAsync(tag).ConfigureAwait(false);
            for (int index = 0; index < values.Length; index++) tag.SetBit(index, values[index]);
            await tag.WriteAsync().ConfigureAwait(false);
            return true;
        });
    }

    static void ValidateBoolIndex(BoolTagSession session, int index)
    {
        if (index < 0 || index >= session.Length) throw new ArgumentOutOfRangeException(nameof(index));
    }
}
