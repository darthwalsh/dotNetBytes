Push-Location $PSScriptRoot

$bytes = @(0..255)
# $bytes += @(0) * 16
# $bytes += @(0..255) | ForEach-Object { @($_) * 16 }

@{
  Name        = "FileFormat"
  Description = ""
  Value       = ""
  Start       = 0
  End         = $bytes.Count
  LinkPath    = ""
  Errors      = @()
  Children    = @()
} | ConvertTo-Json | Set-Content bytes.json

Set-Content Program.dat ([byte[]]$bytes) -AsByteStream

Pop-Location
