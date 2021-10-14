Push-Location $PSScriptRoot

Set-Content bytes.json '{ "Name": "FileFormat"  , "Description": ""  , "Value": ""  , "Start": 0  , "End": 256  , "LinkPath": null  , "Errors": []  , "Children": [] }'

Set-Content Program.dat ([byte[]]@(0..255)) -AsByteStream

Pop-Location
