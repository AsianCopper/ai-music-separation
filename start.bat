@echo off
title Music Remixer

echo Starting Music Remixer...

REM ---- Backend ----
start "Music Remixer - Backend" cmd /c "cd /d %~dp0ai-service && call venv\Scripts\activate && uvicorn main:app --host 127.0.0.1 --port 8000"

REM ---- Frontend ----
start "Music Remixer - Frontend" cmd /c "cd /d %~dp0frontend && npm run dev"

REM ---- Wait for servers, then open browser ----
timeout /t 5 /nobreak >nul
start http://localhost:5173

echo.
echo Backend:  http://127.0.0.1:8000
echo Frontend: http://localhost:5173
echo.
echo Close this window to stop.
pause
