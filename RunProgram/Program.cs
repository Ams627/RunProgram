using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace RunProgram;

internal class RunningProgram
{
    private Process _process { get; set; }
    private readonly BlockingCollection<string> _processOutput = new();

    public RunningProgram(Process process)
    {
        _process = process;
        _process.Exited += Process_Exited;
        _process.OutputDataReceived += Process_OutputDataReceived;
        _process.BeginOutputReadLine();
        _process.WaitForExit();
    }

    private void Process_Exited(object sender, EventArgs e)
    {
        Console.WriteLine("Exited");
        _processOutput.CompleteAdding();
    }

    public IEnumerable<string> GetOutput()
    {
        foreach (var output in _processOutput.GetConsumingEnumerable())
        {
            yield return output;
        }
    }

    private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            _processOutput.Add(e.Data);
        }
    }

    public int ExitCode => _process.ExitCode;
}

internal class ProgramExecutor
{
    private readonly string _programName;
    public ProgramExecutor(string programName)
    {
        _programName = programName;
    }

    public RunningProgram Execute(string args, bool redirectStdOut, bool redirectStdErr)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _programName,
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
        };

        psi.RedirectStandardOutput = redirectStdOut;
        psi.RedirectStandardError = redirectStdErr;
        var process = Process.Start(psi);

        var running = new RunningProgram(process);
        return running;
    }
}

internal class Program
{
    private static void Main(string[] args)
    {
        try
        {
            var prog = new ProgramExecutor("cmd");
            var runningProgram = prog.Execute("/c echo 1 && echo 2 && echo 3", true, false);
            foreach (var str in runningProgram.GetOutput())
            {
                Console.WriteLine(str);
            }
        }
        catch (Exception ex)
        {
            var fullname = System.Reflection.Assembly.GetEntryAssembly().Location;
            var progname = Path.GetFileNameWithoutExtension(fullname);
            Console.Error.WriteLine($"{progname} Error: {ex.Message}");
        }
    }
}
