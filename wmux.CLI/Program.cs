using System.CommandLine;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

/// <summary>
/// wmux CLI — send commands to the running wmux app via Named Pipe.
/// Mirrors cmux's CLI tool.
///
/// Usage:
///   wmux new                        # new workspace
///   wmux split -h / -v              # split pane
///   wmux notify "Title" "Body"      # send notification
///   wmux open "https://..."         # open browser panel
///   wmux keys "text"                # send keys to active terminal
/// </summary>

const string PipeName = "wmux";

var rootCmd = new RootCommand("wmux — AI-friendly terminal for Windows");

// wmux new [--cwd path]
var newCmd = new Command("new", "Open a new workspace");
var cwdOpt = new Option<string?>("--cwd", "Working directory");
newCmd.AddOption(cwdOpt);
newCmd.SetHandler(async (string? cwd) =>
{
    var args = new Dictionary<string, string>();
    if (cwd is not null) args["cwd"] = cwd;
    await SendCommand("new-workspace", args);
}, cwdOpt);

// wmux split -h / -v
var splitCmd = new Command("split", "Split current pane");
var horizOpt = new Option<bool>("--horizontal", "-h", "Split horizontally");
var vertOpt  = new Option<bool>("--vertical",   "-v", "Split vertically");
splitCmd.AddOption(horizOpt);
splitCmd.AddOption(vertOpt);
splitCmd.SetHandler(async (bool h, bool v) =>
{
    await SendCommand(h ? "split-horizontal" : "split-vertical");
}, horizOpt, vertOpt);

// wmux notify <title> <body>
var notifyCmd = new Command("notify", "Send a notification");
var titleArg = new Argument<string>("title");
var bodyArg  = new Argument<string>("body", () => "");
notifyCmd.AddArgument(titleArg);
notifyCmd.AddArgument(bodyArg);
notifyCmd.SetHandler(async (string title, string body) =>
{
    await SendCommand("notify", new() { ["title"] = title, ["body"] = body });
}, titleArg, bodyArg);

// wmux open <url>
var openCmd = new Command("open", "Open a browser panel");
var urlArg = new Argument<string>("url");
openCmd.AddArgument(urlArg);
openCmd.SetHandler(async (string url) =>
{
    await SendCommand("open-browser", new() { ["url"] = url });
}, urlArg);

rootCmd.AddCommand(newCmd);
rootCmd.AddCommand(splitCmd);
rootCmd.AddCommand(notifyCmd);
rootCmd.AddCommand(openCmd);

return await rootCmd.InvokeAsync(args);

// ── Helpers ───────────────────────────────────────────────────────────────────

static async Task SendCommand(string action, Dictionary<string, string>? cmdArgs = null)
{
    try
    {
        using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(2000); // 2s timeout

        var cmd = new { action, args = cmdArgs };
        var json = JsonSerializer.Serialize(cmd);
        var bytes = Encoding.UTF8.GetBytes(json);
        await pipe.WriteAsync(bytes);

        // Read ACK
        var ack = new byte[64];
        await pipe.ReadAsync(ack);
        Console.WriteLine("ok");
    }
    catch (TimeoutException)
    {
        Console.Error.WriteLine("wmux is not running.");
        Environment.Exit(1);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
}
