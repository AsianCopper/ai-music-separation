using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Launcher
{
    static string root;

    static void Log(string msg) { Console.WriteLine("[{0:HH:mm:ss}] {1}", DateTime.Now, msg); }
    static void Error(string msg) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(msg); Console.ResetColor(); }

    static bool PortInUse(int port)
    {
        try { using (var s = new TcpClient("127.0.0.1", port)) { return true; } }
        catch { return false; }
    }

    static string FindNpm()
    {
        // Check common node install paths
        string[] candidates = {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", "npx.cmd"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "nodejs", "npx.cmd"),
            @"C:\Program Files\nodejs\npx.cmd",
        };
        foreach (var p in candidates)
            if (File.Exists(p)) return p;
        return "npx"; // fallback
    }

    static void Main()
    {
        Console.Title = "Music Remixer";
        Console.WriteLine("================================================");
        Console.WriteLine("  Music Remixer Launcher");
        Console.WriteLine("================================================");
        Console.WriteLine();

        root = AppDomain.CurrentDomain.BaseDirectory;

        // ---- Backend ----
        var backendDir = Path.Combine(root, "ai-service");
        var venvPython = Path.Combine(backendDir, "venv", "Scripts", "python.exe");

        if (!Directory.Exists(backendDir))
        {
            Error("ai-service directory not found.");
        }
        else if (!File.Exists(venvPython))
        {
            Error("Backend venv not found. Run:");
            Console.WriteLine("  cd ai-service");
            Console.WriteLine("  python -m venv venv");
            Console.WriteLine("  venv\\Scripts\\activate && pip install -r requirements.txt");
        }
        else if (PortInUse(8000))
        {
            Log("Backend already running on port 8000, skipping.");
        }
        else
        {
            Log("Starting backend...");
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = "/c \"\"" + venvPython + "\" -m uvicorn main:app --host 127.0.0.1 --port 8000\"",
                    WorkingDirectory = backendDir,
                    UseShellExecute = false,
                });
            }
            catch (Exception ex) { Error("Failed to start backend: " + ex.Message); }
        }

        // ---- Frontend ----
        var frontendDir = Path.Combine(root, "frontend");
        var nodeModules = Path.Combine(frontendDir, "node_modules");
        var npmExe = FindNpm();

        if (!Directory.Exists(frontendDir))
        {
            Error("frontend directory not found.");
        }
        else if (!Directory.Exists(nodeModules))
        {
            Error("node_modules not found. Run:");
            Console.WriteLine("  cd frontend");
            Console.WriteLine("  npm install");
        }
        else if (PortInUse(5173))
        {
            Log("Frontend already running on port 5173, skipping.");
        }
        else
        {
            Log("Starting frontend...");
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = "/c \"\"" + npmExe + "\" vite --port 5173\"",
                    WorkingDirectory = frontendDir,
                    UseShellExecute = false,
                });
            }
            catch (Exception ex) { Error("Failed to start frontend: " + ex.Message); }
        }

        // ---- Open browser ----
        Console.WriteLine();
        Log("Opening browser...");
        Thread.Sleep(2000);
        Process.Start(new ProcessStartInfo
        {
            FileName = "http://localhost:5173",
            UseShellExecute = true,
        });

        Console.WriteLine();
        Console.WriteLine("================================================");
        Console.WriteLine("  Backend:  http://127.0.0.1:8000");
        Console.WriteLine("  Frontend: http://localhost:5173");
        Console.WriteLine("================================================");
        Console.Beep();
        Console.WriteLine("Press any key to close this window (servers stay running).");
        Console.ReadKey();
    }
}
