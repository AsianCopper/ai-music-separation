using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

class Launcher
{
    static void Log(string msg) { Console.WriteLine("[{0:HH:mm:ss}] {1}", DateTime.Now, msg); }
    static void Error(string msg) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(msg); Console.ResetColor(); }

    static void Main()
    {
        Console.Title = "Music Remixer";
        Console.WriteLine("================================================");
        Console.WriteLine("  Music Remixer Launcher");
        Console.WriteLine("================================================");
        Console.WriteLine();

        var root = AppDomain.CurrentDomain.BaseDirectory;

        // ---- Backend ----
        var backendDir = Path.Combine(root, "ai-service");
        var venvPython = Path.Combine(backendDir, "venv", "Scripts", "python.exe");
        var reqTxt = Path.Combine(backendDir, "requirements.txt");

        if (!Directory.Exists(backendDir))
        {
            Error("[ERROR] ai-service directory not found. Are you running this from the project root?");
        }
        else if (!File.Exists(venvPython))
        {
            Error("[ERROR] Backend venv not found.");
            Console.WriteLine("  Run these commands first:");
            Console.WriteLine("    cd ai-service");
            Console.WriteLine("    python -m venv venv");
            Console.WriteLine("    venv\\Scripts\\activate");
            Console.WriteLine("    pip install -r requirements.txt");
        }
        else
        {
            Log("Starting backend...");
            Process.Start(new ProcessStartInfo
            {
                FileName = venvPython,
                Arguments = "-m uvicorn main:app --host 127.0.0.1 --port 8000",
                WorkingDirectory = backendDir,
                UseShellExecute = false,
            });
        }

        // ---- Frontend ----
        var frontendDir = Path.Combine(root, "frontend");
        var nodeModules = Path.Combine(frontendDir, "node_modules");

        if (!Directory.Exists(frontendDir))
        {
            Error("[ERROR] frontend directory not found.");
        }
        else if (!Directory.Exists(nodeModules))
        {
            Error("[ERROR] node_modules not found.");
            Console.WriteLine("  Run these commands first:");
            Console.WriteLine("    cd frontend");
            Console.WriteLine("    npm install");
        }
        else
        {
            Log("Starting frontend...");
            Process.Start(new ProcessStartInfo
            {
                FileName = "npx",
                Arguments = "vite --port 5173",
                WorkingDirectory = frontendDir,
                UseShellExecute = false,
            });
        }

        // ---- Open browser ----
        Console.WriteLine();
        Log("Waiting for servers to start...");
        Thread.Sleep(5000);
        Log("Opening browser...");
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
        Console.WriteLine("Press any key to close this window (servers will keep running)...");
        Console.ReadKey();
    }
}
