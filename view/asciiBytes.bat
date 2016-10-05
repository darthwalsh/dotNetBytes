@echo off
pushd "%~dp0"

echo { "Name": "FileFormat"  , "Description": ""  , "Value": ""  , "Start": 0  , "End": 256  , "LinkPath": null  , "Errors": []  , "Children": [] } > bytes.json

powershell -command "[System.IO.File]::WriteAllBytes('Program.dat', @(0..255))"

popd

