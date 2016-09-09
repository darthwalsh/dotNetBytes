@echo off
pushd "%~dp0"

start "" ..\bin\Debug\dotNetBytes.exe Program.dat

REM sleep for a few seconds
ping -n 3 127.0.0.1 > nul
del bytes.json
curl http://127.0.0.1:8000/Content/bytes.json | json-prettify > bytes.json

taskkill /im dotNetBytes.exe

popd

