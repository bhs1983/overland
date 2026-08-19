@echo off
rem Open Overland in the Godot .NET editor (GL Compatibility). Detached — safe from parent Job Objects.
set DOTNET_ROOT=D:\tools\dotnet-sdk-8.0.424
set PATH=%DOTNET_ROOT%;%PATH%
set GODOT=D:\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe
if not exist "%GODOT%" set GODOT=D:\tools\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe
cd /d D:\Overland
start "Overland Editor" "%GODOT%" --path D:\Overland --editor --rendering-method gl_compatibility res://scenes/Game.tscn
