using PLC_COMM_API;
using libplctag;

var api = new API_UI("127.0.0.1");
api.AddVariable("Jog", 2);
await api.SetVariableBool("Jog", true);
if (Tag.Writes != 6) throw new Exception("Expected six legacy writes.");
Console.WriteLine("REPRODUCED: one successful scalar command produces 6 writes.");
api.CloseConnection();

api = new API_UI("127.0.0.1");
api.AddVariable("Commands", 2, 33);
var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
Tag.WriteGate = gate.Task;
var indexed = api.SetIndexArrayBool("Commands", 32, true);
await Task.Delay(20);
if (!indexed.IsCompleted) throw new Exception("Expected premature legacy completion.");
Console.WriteLine("REPRODUCED: indexed BOOL setter finishes while its write is still blocked.");
gate.SetResult();
await Task.Delay(20);
Tag.WriteGate = null;
api.CloseConnection();

api = new API_UI("127.0.0.1");
api.AddVariable("Jog", 2);
gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var firstWrite = true;
Tag.BeforeWrite = value => { if (value && firstWrite) { firstWrite = false; return gate.Task; } return Task.CompletedTask; };
var press = api.SetVariableBool("Jog", true);
await api.SetVariableBool("Jog", false);
gate.SetResult();
await press;
if (!Tag.PlcValue) throw new Exception("Expected legacy release overwritten by press.");
Console.WriteLine("REPRODUCED: delayed press completes after release; final PLC BOOL is TRUE.");
api.CloseConnection();
