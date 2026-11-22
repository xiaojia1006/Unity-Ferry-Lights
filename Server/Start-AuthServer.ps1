param([int]$Port = 18765)
$ErrorActionPreference = 'Stop'
$bundledPython = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$pythonCommand = Get-Command python -ErrorAction SilentlyContinue
$pythonExe = if ($pythonCommand) { $pythonCommand.Source } elseif (Test-Path -LiteralPath $bundledPython) { $bundledPython } else { throw 'Please install Python 3.10 or newer.' }
$health = $null
try { $health = Invoke-RestMethod -Uri "http://127.0.0.1:$Port/health" -TimeoutSec 2 } catch { }
if ($health) {
    if ($health.service -eq 'willowreach-local-auth') { Write-Output "Account service already running on port $Port."; return }
    throw "Port $Port is used by another service."
}
Start-Process -FilePath $pythonExe -ArgumentList @('"' + (Join-Path $PSScriptRoot 'auth_server.py') + '"', '--port', $Port) -WorkingDirectory $PSScriptRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $PSScriptRoot 'service.log') -RedirectStandardError (Join-Path $PSScriptRoot 'service-error.log') | Out-Null
Write-Output "Starting account service on 127.0.0.1:$Port."
