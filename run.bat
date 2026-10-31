@echo off
chcp 65001
pushd "%~dp0"
dotnet build || (popd & exit /b 1)
start "" godot --path "." -- --museum
godot --path "."
popd
