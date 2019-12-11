Param ([string]$Version = "0.1-debug")
$ErrorActionPreference = "Stop"
pushd $(Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)

dotnet msbuild /t:Restore /t:Build /p:Configuration=Release /p:Version=$Version
if ($LASTEXITCODE -ne 0) {throw "Exit Code: $LASTEXITCODE"}

popd
