@echo off
echo Запускаем LocalMimu...

:: 1. Заходим в папку сервера и открываем его в новом окне
cd MimuServer
start "SERVER" cmd /k "dotnet run"

:: 2. Ждем 2 секунды, чтобы сервер успел запуститься
timeout /t 2 /nobreak > nul

:: 3. Выходим обратно в общую папку и заходим в клиент
cd ..
cd MimuClient

:: 4. Запускаем два клиента в разных окнах
start "CLIENT 1 - ALEX" cmd /k "dotnet run"
timeout /t 3 /nobreak > nul
start "CLIENT 2 - KEJDO" cmd /k "dotnet run"

exit