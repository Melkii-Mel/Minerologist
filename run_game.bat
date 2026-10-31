@echo off
chcp 65001
pushd "%~dp0"
dotnet build || (popd & exit /b 1)
godot --path "."
popd

