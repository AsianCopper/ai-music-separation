using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;

class Launcher
{
    static string root;
    static List<Process> children = new List<Process>();
    static bool running = true;
    static System.Text.RegularExpressions.Regex ansiRegex =
        new System.Text.RegularExpressions.Regex(@"\x1b\[[0-9;]*m");

    static void Log(string msg)
    {
        Console.WriteLine("[{0:HH:mm:ss}] {1}", DateTime.Now, msg);
    }

    static string Clean(string s)
    {
        s = ansiRegex.Replace(s, "");
        // Replace common Unicode chars that cmd.exe can't render
        s = s.Replace('➜', '>');  // ➜
        s = s.Replace('✔', 'v');  // ✔
        s = s.Replace('✖', 'x');  // ✖
        return s;
    }

    static bool PortInUse(int port)
    {
        try { using (var s = new TcpClient("127.0.0.1", port)) { return true; } }
        catch { return false; }
    }

    static string FindNode()
    {
        string[] dirs = {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "nodejs"),
            @"C:\Program Files\nodejs",
        };
        foreach (var d in dirs)
        {
            var npx = Path.Combine(d, "npx.cmd");
            if (File.Exists(npx)) return npx;
        }
        return "npx";
    }

    static Process Launch(string exe, string args, string workDir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            WorkingDirectory = workDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        psi.EnvironmentVariables["NO_COLOR"] = "1";
        psi.EnvironmentVariables["FORCE_COLOR"] = "0";
        var proc = new Process { StartInfo = psi };
        proc.OutputDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(Clean(e.Data)); };
        proc.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(Clean(e.Data)); };
        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        children.Add(proc);
        return proc;
    }

    static void KillPort(int port)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-NoProfile -Command \"Get-NetTCPConnection -LocalPort " + port + " -State Listen -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            var p = Process.Start(psi);
            p.WaitForExit(3000);
        }
        catch { }
    }

    static void KillChildren()
    {
        foreach (var c in children)
            try { if (!c.HasExited) c.Kill(); } catch { }
    }

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "Music Remixer — do not close this window";
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; running = false; };

        Console.WriteLine("================================================");
        Console.WriteLine("  Music Remixer");
        Console.WriteLine("  Close this window to stop all servers.");
        Console.WriteLine("================================================");
        Console.WriteLine();

        root = AppDomain.CurrentDomain.BaseDirectory;

        // ---- Backend ----
        var backendDir = Path.Combine(root, "ai-service");
        var venvPython = Path.Combine(backendDir, "venv", "Scripts", "python.exe");

        if (!File.Exists(venvPython))
        {
            Console.WriteLine("[!] Backend venv not found. Skipping backend.");
            Console.WriteLine("    Run: cd ai-service && python -m venv venv && pip install -r requirements.txt");
        }
        else if (PortInUse(8000))
        {
            Log("Port 8000 in use, killing old process...");
            KillPort(8000);
        }
        else
        {
            Log("Starting backend (FastAPI :8000)...");
            Launch(
                Path.Combine(backendDir, "venv", "Scripts", "python.exe"),
                "-u -m uvicorn main:app --host 127.0.0.1 --port 8000",
                backendDir
            );
        }

        // ---- Frontend ----
        var frontendDir = Path.Combine(root, "frontend");
        var nodeModules = Path.Combine(frontendDir, "node_modules");
        var npx = FindNode();

        if (!Directory.Exists(nodeModules))
        {
            Console.WriteLine("[!] node_modules not found. Skipping frontend.");
            Console.WriteLine("    Run: cd frontend && npm install");
        }
        else if (PortInUse(5173))
        {
            Log("Port 5173 in use, killing old process...");
            KillPort(5173);
        }
        else
        {
            Log("Starting frontend (Vite :5173)...");
            Launch(npx, "vite --port 5173", frontendDir);
        }

        // ---- Open browser ----
        Thread.Sleep(3000);
        try { Process.Start(new ProcessStartInfo("http://localhost:5173", "") { UseShellExecute = true }); } catch { }

        Console.WriteLine();
        Console.WriteLine("================================================");
        Console.WriteLine("  All servers running. Close this window to stop.");
        Console.WriteLine("  Backend:  http://127.0.0.1:8000");
        Console.WriteLine("  Frontend: http://localhost:5173");
        Console.WriteLine("================================================");
        Console.WriteLine();

        // Block until user closes the window
        while (running)
        {
            if (children.TrueForAll(c => c.HasExited))
                break;
            Thread.Sleep(1000);
        }

        Console.WriteLine("Shutting down...");
        KillChildren();
        Console.WriteLine("Done.");
    }
}
