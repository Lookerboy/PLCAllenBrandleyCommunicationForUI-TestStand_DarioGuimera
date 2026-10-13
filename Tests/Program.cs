using PLC_COMM_API;
using View.Plc;
using Controlar.WinFormsLibrary.Components;
using System.Reflection;
using System.Runtime.Loader;
using System.Windows.Forms;

static class Program
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static async Task Error(Func<Task> action)
    { try { await action(); } catch { return; } throw new Exception("Expected an error."); }
    static TaskCompletionSource Gate() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    static (API_UI Api, FakeBoolTag Tag, ManualPlcController Manual) Fixture(int length = 1, Func<bool>? ready = null)
    {
        FakeBoolTag? tag = null;
        var api = new API_UI("127.0.0.1", options => tag = new FakeBoolTag(options));
        api.AddVariable("Cmd", 2, length);
        return (api, tag!, new ManualPlcController(new AllenBradleyProvider(api), ready));
    }
    static async Task Scenario(string name, Func<Task> action)
    { await action().WaitAsync(TimeSpan.FromSeconds(20)); Console.WriteLine("PASS: " + name); }

    [STAThread]
    static void Main(string[] args)
    {
        if (args.Contains("--debug")) libplctag.LibPlcTag.DebugLevel = libplctag.DebugLevel.Detail;
        if (args.Length > 0 && args[0] == "--local-eip") { LocalEipAsync().GetAwaiter().GetResult(); LocalGui(); return; }
        if (args.Length == 2 && args[0] == "--abi") { CompareAbi(args[1]); return; }
        DriverAndManualAsync().GetAwaiter().GetResult();
        Gui();
        Console.WriteLine("PASS: offline driver + manual commands + hidden Controlar WinForms events; no PLC/NI.");
    }

    static async Task DriverAndManualAsync()
    {
        await Scenario("one successful BOOL command writes once; logical lengths 1/31/32/33 and bounds", async () =>
        {
            foreach (int length in new[] { 1, 31, 32, 33 })
            {
                var f = Fixture(length);
                await f.Api.SetIndexArrayBool("Cmd", length - 1, true);
                Check(f.Tag.Writes.Count == 1 && f.Tag.Plc[length - 1], "Repeated/missing write.");
                Check((await f.Api.GetVariableArrayBool("Cmd")).Count == length, "Packed capacity leaked as logical length.");
                await Error(() => f.Api.SetIndexArrayBool("Cmd", -1, true));
                await Error(() => f.Api.SetIndexArrayBool("Cmd", length, true));
                f.Api.AddVariable("cmd", 2, length); // Case aliases share the tag and gate.
                await Error(() => { f.Api.AddVariable("Cmd", 2, length + 1); return Task.CompletedTask; });
                f.Api.CloseConnection();
                f.Api.CloseConnection();
                Check(f.Tag.DisposeCount == 1, "Close was not idempotent.");
            }
        });
        await Scenario("delayed press cannot overwrite release; polling never overlaps its tag", async () =>
        {
            var f = Fixture(); var started = Gate(); var unblock = Gate();
            f.Tag.BeforeWrite = _ => { started.TrySetResult(); return unblock.Task; };
            var press = f.Api.SetVariableBool("Cmd", true);
            await started.Task;
            var release = f.Api.SetVariableBool("Cmd", false);
            var read = f.Api.GetVariableBool("Cmd");
            bool waiting = !release.IsCompleted && !read.IsCompleted;
            unblock.SetResult();
            await Task.WhenAll(press, release, read);
            Check(waiting && !f.Tag.Plc[0] && f.Tag.Writes.Count == 2 && f.Tag.MaxInFlight == 1, "Write ordering/buffer corruption.");
            f.Api.CloseConnection();
        });
        await Scenario("indexed setter awaits write and propagates failure; adjacent/padding bits preserved", async () =>
        {
            var f = Fixture(33); var started = Gate(); var unblock = Gate();
            f.Tag.Plc[63] = true;
            f.Tag.BeforeWrite = _ => { started.TrySetResult(); return unblock.Task; };
            var write = f.Api.SetIndexArrayBool("Cmd", 32, true);
            await started.Task;
            bool waiting = !write.IsCompleted;
            unblock.SetResult(); await write;
            f.Tag.BeforeWrite = null;
            await Task.WhenAll(f.Api.SetIndexArrayBool("Cmd", 0, true), f.Api.SetIndexArrayBool("Cmd", 31, true));
            Check(waiting && f.Tag.Plc[0] && f.Tag.Plc[31] && f.Tag.Plc[32] && f.Tag.Plc[63], "Indexed write lost a bit.");
            f.Tag.WriteFailures = 1;
            int count = f.Tag.Writes.Count;
            await Error(() => f.Api.SetIndexArrayBool("Cmd", 0, false));
            Check(f.Tag.Writes.Count == count + 1, "Failed write replayed automatically.");
            f.Tag.ReadFailures = 2;
            Check(await f.Api.GetIndexArrayBool("Cmd", 32), "Read retry corrupted value.");
            f.Api.CloseConnection();
        });
        await Scenario("close waits active write, rejects new requests and disposes once", async () =>
        {
            var f = Fixture(); var started = Gate(); var unblock = Gate();
            f.Tag.BeforeWrite = _ => { started.TrySetResult(); return unblock.Task; };
            var write = f.Api.SetVariableBool("Cmd", true); await started.Task;
            var close = Task.Run(f.Api.CloseConnection);
            await Task.Delay(20);
            bool waiting = !close.IsCompleted && f.Tag.DisposeCount == 0;
            unblock.SetResult(); await Task.WhenAll(write, close);
            await Error(() => f.Api.GetVariableBool("Cmd"));
            Check(waiting && f.Tag.DisposeCount == 1, "Disposed active tag.");
        });
        await Scenario("1001 rapid toggles and absolute true/false actions retain their order", async () =>
        {
            var f = Fixture(); var binding = new PlcBoolBinding("Cmd");
            await Task.WhenAll(Enumerable.Range(0, 1001).Select(_ => f.Manual.ClickAsync(binding, ManualBoolAction.Toggle)));
            Check(f.Tag.Plc[0] && f.Tag.Writes.Count == 1001, "Concurrent toggles lost transitions.");
            await Task.WhenAll(f.Manual.ClickAsync(binding, ManualBoolAction.SetTrue), f.Manual.ClickAsync(binding, ManualBoolAction.SetFalse));
            Check(!f.Tag.Plc[0], "Absolute actions reordered.");
            await f.Manual.StopAsync(); f.Api.CloseConnection();
        });
        await Scenario("600 held TAP cycles: toggle/true/false restore both initial values", async () =>
        {
            var f = Fixture(); var binding = new PlcBoolBinding("Cmd");
            foreach (bool initial in new[] { false, true })
            foreach (var action in Enum.GetValues<ManualBoolAction>())
            for (int n = 0; n < 100; n++)
            {
                f.Tag.Plc[0] = initial;
                await f.Manual.PressAsync("Jog", binding, action);
                await f.Manual.PressAsync("Jog", binding, action); // Keyboard autorepeat.
                await f.Api.GetVariableBool("Cmd"); // Polling must never change the captured return value.
                await f.Manual.ReleaseAsync("Jog");
                Check(f.Tag.Plc[0] == initial && !f.Manual.IsEngaged("Jog"), "TAP failed to restore its captured baseline.");
            }
            Check(f.Tag.Writes.Count == 1200, "Autorepeat produced extra commands.");
            await f.Manual.StopAsync(); f.Api.CloseConnection();
        });
        await Scenario("release during delayed baseline read cancels activation", async () =>
        {
            var f = Fixture(); var started = Gate(); var unblock = Gate();
            f.Tag.BeforeRead = () => { started.TrySetResult(); return unblock.Task; };
            var press = f.Manual.PressAsync("Jog", new("Cmd"), ManualBoolAction.SetTrue);
            await started.Task; var release = f.Manual.ReleaseAsync("Jog");
            unblock.SetResult(); await Task.WhenAll(press, release);
            Check(f.Tag.Writes.Count == 0 && !f.Tag.Plc[0], "Late activation after release.");
            await f.Manual.StopAsync(); f.Api.CloseConnection();
        });
        await Scenario("release during in-flight activation restores after its acknowledgement", async () =>
        {
            var f = Fixture(); var started = Gate(); var unblock = Gate();
            f.Tag.BeforeWrite = _ => { started.TrySetResult(); return unblock.Task; };
            var press = f.Manual.PressAsync("Jog", new("Cmd"), ManualBoolAction.SetTrue);
            await started.Task; var release = f.Manual.ReleaseAsync("Jog");
            unblock.SetResult(); await Task.WhenAll(press, release);
            Check(!f.Tag.Plc[0] && f.Tag.Writes.Count == 2, "In-flight press overtook release.");
            await f.Manual.StopAsync(); f.Api.CloseConnection();
        });
        await Scenario("uncertain activation acknowledgement triggers restoration and reports error", async () =>
        {
            var f = Fixture(); f.Tag.WriteFailures = 1; f.Tag.ApplyBeforeWriteError = true;
            await Error(() => f.Manual.PressAsync("Jog", new("Cmd"), ManualBoolAction.SetTrue));
            Check(!f.Tag.Plc[0] && f.Tag.Writes.Count == 2, "Applied but failed press stayed TRUE.");
            await f.Manual.StopAsync(); f.Api.CloseConnection();
        });
        await Scenario("release retries once; persistent failure remains pending and can recover when disabled", async () =>
        {
            bool ready = true; var f = Fixture(1, () => ready); var binding = new PlcBoolBinding("Cmd");
            await f.Manual.PressAsync("Jog", binding, ManualBoolAction.SetTrue);
            f.Tag.WriteFailures = 1;
            await f.Manual.ReleaseAsync("Jog");
            Check(!f.Tag.Plc[0], "Transient release failed to recover.");
            await f.Manual.PressAsync("Jog", binding, ManualBoolAction.SetTrue);
            f.Tag.WriteFailures = 2;
            await Error(() => f.Manual.ReleaseAsync("Jog"));
            Check(f.Manual.IsEngaged("Jog") && f.Tag.Plc[0], "Failed release reported as successful.");
            await Error(() => f.Manual.PressAsync("Other", binding, ManualBoolAction.SetTrue));
            ready = false;
            await f.Manual.ReleaseAsync("Jog");
            Check(!f.Tag.Plc[0], "Activation guard prevented restoration.");
            await f.Manual.StopAsync(); f.Api.CloseConnection();
        });
        await Scenario("independent array jog bits and close restore before disposal", async () =>
        {
            var f = Fixture(33);
            await Task.WhenAll(f.Manual.PressAsync("JogPlus", new("Cmd", 0), ManualBoolAction.SetTrue),
                f.Manual.PressAsync("JogMinus", new("Cmd", 32), ManualBoolAction.SetTrue));
            Check(f.Tag.Plc[0] && f.Tag.Plc[32], "A jog overwrote another bit.");
            await Error(() => f.Manual.PressAsync("Duplicate", new("Cmd", 0), ManualBoolAction.SetTrue));
            await Error(() => f.Manual.PressAsync("CaseAlias", new("cmd", 0), ManualBoolAction.SetTrue));
            await f.Manual.StopAsync(); f.Api.CloseConnection();
            Check(!f.Tag.Plc[0] && !f.Tag.Plc[32] && f.Tag.DisposeCount == 1, "Closed without releasing jog bits.");
        });
        await Scenario("close during delayed activation waits restoration and cancels later clicks", async () =>
        {
            var f = Fixture(); var started = Gate(); var unblock = Gate();
            f.Tag.BeforeWrite = _ => { started.TrySetResult(); return unblock.Task; };
            var press = f.Manual.PressAsync("Jog", new("Cmd"), ManualBoolAction.SetTrue);
            await started.Task; var stop = f.Manual.StopAsync();
            var lateClick = f.Manual.ClickAsync(new("Cmd"), ManualBoolAction.SetTrue);
            unblock.SetResult(); await Task.WhenAll(press, stop, lateClick);
            f.Api.CloseConnection();
            Check(!f.Tag.Plc[0] && f.Tag.Writes.Count == 2 && f.Tag.DisposeCount == 1, "Close left an activation behind.");
        });
        await Scenario("explicit OFF supersedes held TAP and later release cannot reactivate", async () =>
        {
            var f = Fixture(); var binding = new PlcBoolBinding("Cmd");
            f.Tag.Plc[0] = true;
            await f.Manual.PressAsync("Tap", binding, ManualBoolAction.SetFalse);
            await f.Manual.ClickAsync(binding, ManualBoolAction.SetFalse);
            await f.Manual.ReleaseAsync("Tap");
            Check(!f.Tag.Plc[0] && !f.Manual.IsEngaged("Tap"), "MouseUp overwrote explicit OFF with old TRUE baseline.");
            await f.Manual.PressAsync("Tap", binding, ManualBoolAction.SetTrue);
            f.Tag.WriteFailures = 1;
            await Error(() => f.Manual.ClickAsync(binding, ManualBoolAction.SetFalse));
            Check(!f.Tag.Plc[0], "Failed OFF left held jog on.");
            await f.Manual.ReleaseAsync("Tap");
            await f.Manual.StopAsync(); f.Api.CloseConnection();
        });
    }

    sealed class TestButton : View_NS.CtrlButtonInterceptor
    {
        public void Down(MouseButtons button = MouseButtons.Left) => OnMouseDown(new(button, 1, 10, 10, 0));
        public void Up() => OnMouseUp(new(MouseButtons.Left, 1, 10, 10, 0));
        public void Outside() => OnMouseMove(new(MouseButtons.Left, 0, -10, -10, 0));
        public void LoseFocus() => OnLostFocus(EventArgs.Empty);
        public void HideSignal() { Hide(); OnVisibleChanged(EventArgs.Empty); }
        public void KeyPress() => OnKeyDown(new(Keys.Space));
        public void KeyRelease() => OnKeyUp(new(Keys.Space));
    }
    sealed class TestForm : Form { public void LoseActivation() => OnDeactivate(EventArgs.Empty); }
    static void Pump(Func<bool> completed)
    {
        var deadline = DateTime.UtcNow.AddSeconds(8);
        while (!completed()) { if (DateTime.UtcNow > deadline) throw new TimeoutException("Hidden UI test timed out."); Application.DoEvents(); Thread.Sleep(1); }
        Application.DoEvents();
    }
    static void Pump(Task task) { Pump(() => task.IsCompleted); task.GetAwaiter().GetResult(); }
    static void Gui()
    {
        var readOnly = new ControlDefinition { ButtonRead = true };
        Check(ManualPlcButtonBinding.ShouldReadEnabled(readOnly), "Status-only button lost ButtonRead behavior.");
        foreach (string plcAction in new[] { "TogglePLCVariable", "SetToTruePLCVariable", "SetToFalsePLCVariable" })
        {
            var definition = new ControlDefinition { ButtonRead = true, Events = new() { ["click"] = new() { plcAction } } };
            Check(!ManualPlcButtonBinding.ShouldReadEnabled(definition) && !ManualPlcButtonBinding.IsMomentary(definition), "Manual OFF/toggle could be disabled by its own flag.");
            definition.Events["MouseDown"] = new() { plcAction };
            Check(ManualPlcButtonBinding.IsMomentary(definition), "Combined click/TAP was not classified as momentary.");
        }
        Console.WriteLine("PASS: ButtonRead preserves status-only gating and cannot disable manual OFF/toggle; TAP classification wins over click.");
        foreach (bool initial in new[] { false, true })
        foreach (var action in Enum.GetValues<ManualBoolAction>())
        foreach (string reason in new[] { "MouseUp", "outside", "capture loss", "focus loss", "disable", "deactivate", "hide", "dispose", "keyboard" })
        {
            var f = Fixture(); int errors = 0;
            f.Tag.Plc[0] = initial;
            using var form = new TestForm();
            var button = new TestButton { Name = "Jog", Size = new(100, 40) };
            form.Controls.Add(button); _ = form.Handle; _ = button.Handle;
            using var binding = new ManualPlcButtonBinding(button, new("Cmd"), action, () => f.Manual, _ => errors++);
            button.Down(MouseButtons.Right); Check(!f.Manual.IsEngaged("Jog"), "Right click activated jog.");
            if (reason == "keyboard") { button.KeyPress(); button.KeyPress(); } else button.Down();
            bool held = action == ManualBoolAction.Toggle ? !initial : action == ManualBoolAction.SetTrue;
            Pump(() => f.Tag.Writes.Count == 1 && f.Tag.Plc[0] == held);
            switch (reason)
            {
                case "MouseUp": button.Up(); break;
                case "outside": button.Outside(); break;
                case "capture loss": button.Capture = false; break;
                case "focus loss": button.LoseFocus(); break;
                case "disable": button.Enabled = false; break;
                case "deactivate": form.LoseActivation(); break;
                case "hide": button.HideSignal(); break;
                case "dispose": button.Dispose(); break;
                case "keyboard": button.KeyRelease(); break;
            }
            Pump(() => !f.Manual.IsEngaged("Jog"));
            Check(f.Tag.Plc[0] == initial && errors == 0 && f.Tag.Writes.Count == 2, "GUI release failed: " + reason);
            Pump(f.Manual.StopAsync()); f.Api.CloseConnection();
        }
        Console.WriteLine("PASS: 54 hidden Controlar input cases: 3 TAP actions x 2 initial values x 9 release signals.");
    }

    static void CompareAbi(string previousDll)
    {
        var context = new AssemblyLoadContext("previous-api", isCollectible: true);
        var previous = context.LoadFromAssemblyPath(Path.GetFullPath(previousDll)).GetType("PLC_COMM_API.API_UI")!;
        static string[] Surface(Type type) => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.ReturnType + " " + m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.ToString())) + ")").Order().ToArray();
        var oldSurface = Surface(previous); var newSurface = Surface(typeof(API_UI));
        foreach (var missing in oldSurface.Except(newSurface)) Console.WriteLine("Previous-only signature: " + missing);
        foreach (var added in newSurface.Except(oldSurface)) Console.WriteLine("Current-only signature: " + added);
        Check(oldSurface.SequenceEqual(newSurface), "Public UI DLL API changed.");
        Check(typeof(API_UI).GetConstructor(new[] { typeof(string) }) != null, "Legacy constructor missing.");
        context.Unload();
        Console.WriteLine("PASS: public API signatures identical to previous DLL.");
    }

    static async Task LocalEipAsync()
    {
        // Always loopback: this mode cannot target a plant PLC.
        var api = new API_UI("127.0.0.1"); api.AddVariable("Jog", 2); api.AddVariable("Commands", 2, 33);
        api.AddVariable("Counter", 7); api.AddVariable("Values", 7, 2); api.AddVariable("Message", 1);
        var numericTags = (List<libplctag.Tag>)typeof(API_UI).GetField("Tags", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(api)!;
        int numericWrites = 0;
        foreach (var tag in numericTags) tag.WriteCompleted += (_, _) => Interlocked.Increment(ref numericWrites);
        await api.SetVariableInt32("Counter", 42);
        await api.SetIndexArrayInt32("Values", 1, 73);
        await api.SetVariableString("Message", "local test");
        Check(numericWrites == 3 && await api.GetVariableInt32("Counter") == 42 &&
            await api.GetIndexArrayInt32("Values", 1) == 73 && await api.GetVariableString("Message") == "local test",
            "UI numeric/STRING writes repeated, ended early or corrupted value.");
        var manual = new ManualPlcController(new AllenBradleyProvider(api));
        await api.SetVariableBool("Jog", false);
        var poll = Task.Run(async () => { for (int i = 0; i < 200; i++) await api.GetVariableBool("Jog"); });
        for (int i = 0; i < 100; i++)
        {
            await manual.PressAsync("Jog", new("Jog"), ManualBoolAction.SetTrue);
            await manual.ReleaseAsync("Jog");
            Check(!await api.GetVariableBool("Jog"), "Native EIP jog stayed TRUE.");
        }
        await poll;
        await Task.WhenAll(Enumerable.Range(0, 101).Select(_ => manual.ClickAsync(new("Jog"), ManualBoolAction.Toggle)));
        Check(await api.GetVariableBool("Jog"), "Native EIP toggles lost updates.");
        await api.SetVariableBool("Jog", false);
        await Task.WhenAll(api.SetIndexArrayBool("Commands", 0, true), api.SetIndexArrayBool("Commands", 32, true));
        Check(await api.GetIndexArrayBool("Commands", 0) && await api.GetIndexArrayBool("Commands", 32), "Native packed-bit update lost a neighbor.");
        await api.SetIndexArrayBool("Commands", 0, false); await api.SetIndexArrayBool("Commands", 32, false);
        await manual.StopAsync(); api.CloseConnection();
        var ts = new PLC_COMM_TESTSTAND_API.API_TESTSTAND("127.0.0.1"); ts.AddVariable("Jog", "BOOL");
        ts.SetVariableBool("Jog", true); Check(ts.GetVariableBool("Jog"), "TestStand facade true failed.");
        ts.SetVariableBool("Jog", false); Check(!ts.GetVariableBool("Jog"), "TestStand facade false failed."); ts.CloseConnection();
        Console.WriteLine("PASS: native loopback EIP: 100 jog cycles + polling + 101 toggles + packed indices + DINT/STRING single writes + unchanged TestStand facade.");
    }

    static void LocalGui()
    {
        var api = new API_UI("127.0.0.1"); api.AddVariable("Jog", 2);
        var manual = new ManualPlcController(new AllenBradleyProvider(api));
        using var form = new TestForm();
        var button = new TestButton { Name = "Jog", Size = new(100, 40) };
        form.Controls.Add(button); _ = form.Handle; _ = button.Handle;
        using var binding = new ManualPlcButtonBinding(button, new("Jog"), ManualBoolAction.SetTrue, () => manual,
            ex => throw new Exception("Native GUI command failed.", ex));
        Pump(api.SetVariableBool("Jog", false));
        for (int n = 0; n < 20; n++)
        {
            button.Down();
            bool pressed = false;
            var deadline = DateTime.UtcNow.AddSeconds(8);
            while (!pressed)
            {
                var read = api.GetVariableBool("Jog"); Pump(read); pressed = read.Result;
                if (DateTime.UtcNow > deadline) throw new TimeoutException("Native GUI activation timed out.");
            }
            if (n % 2 == 0) button.Up(); else button.Capture = false;
            Pump(() => !manual.IsEngaged("Jog"));
            var after = api.GetVariableBool("Jog"); Pump(after); Check(!after.Result, "Native GUI release stayed TRUE.");
        }
        Pump(manual.StopAsync()); api.CloseConnection();
        Console.WriteLine("PASS: 20 end-to-end hidden Controlar jog cycles -> provider -> actual native DLL -> local EIP, alternating MouseUp/capture loss.");
    }
}
