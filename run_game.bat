@echo off
set DOTNET_ROOT=D:\tools\dotnet-sdk-8.0.424
set PATH=%DOTNET_ROOT%;%PATH%
cd /d D:\Overland
start "Overland Slice 0" "D:\tools\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" --path D:\Overland res://scenes/Game.tscn --resolution 1280x720 --rendering-method gl_compatibility --resolution 1280x720
