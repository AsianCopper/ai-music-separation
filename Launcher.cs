using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

class Launcher
{
    static void Main()
    {
        var root = AppDomain.CurrentDomain.BaseDirectory;

        // Backend
        var backendDir = Path.Combine(root, "ai-service");
        var venvPython = Path.Combine(backendDir, "venv", "Scripts", "python.exe");
        if (File.Exists(venvPython))
        {
            var psi = new ProcessStartInfo
            {
                FileName = venvPython,
                Arguments = "-m uvicorn main:app --host 127.0.0.1 --port 8000",
                WorkingDirectory = backendDir,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }

        // Frontend
        var frontendDir = Path.Combine(root, "frontend");
        if (Directory.Exists(Path.Combine(frontendDir, "node_modules")))
        {
            var psi = new ProcessStartInfo
            {
                FileName = "npx",
                Arguments = "vite --port 5173",
                WorkingDirectory = frontendDir,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }

        // Wait and open browser
        Thread.Sleep(6000);
        Process.Start(new ProcessStartInfo
        {
            FileName = "http://localhost:5173",
            UseShellExecute = true
        });
    }
}
