param([string]$UnityEditorData = 'D:/unity/2021.3.45f2c1/Editor/Data')
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$output = Join-Path $root 'Temp/GameplayLogicTests'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$framework = Join-Path $UnityEditorData 'MonoBleedingEdge/lib/mono/4.5'
$sources = @(
    'Inventory&Shop/InventortyManager.cs','Inventory&Shop/InventorySlot.cs',
    'Inventory&Shop/ItemSO.cs','Inventory&Shop/Shop/ShopManager.cs','Inventory&Shop/Useltem.cs',
    'QuestSystem/QuestManager.cs','QuestSystem/QuestSo.cs','QuestSystem/QuestEvents.cs',
    'QuestSystem/QuestLogUI.cs','PlayerScripts/ExpManager.cs','MenuPause.cs'
)
$arguments = @('/target:exe','/nostdlib+','/langversion:9',('/out:"' + (Join-Path $output 'Checks.exe') + '"'))
$arguments += @('mscorlib.dll','System.dll','System.Core.dll') | ForEach-Object { '/reference:"' + (Join-Path $framework $_) + '"' }
$arguments += $sources | ForEach-Object { '"' + (Join-Path $root ('Assets/scrpits/' + $_)) + '"' }
$arguments += @('EngineStubs.cs','RegressionChecks.cs') | ForEach-Object { '"' + (Join-Path $PSScriptRoot $_) + '"' }
$response = Join-Path $output 'compile.rsp'
[IO.File]::WriteAllLines($response, $arguments)
& (Join-Path $UnityEditorData 'NetCoreRuntime/dotnet.exe') (Join-Path $UnityEditorData 'DotNetSdkRoslyn/csc.dll') ('@' + $response)
if ($LASTEXITCODE -ne 0) { throw 'Logic test compilation failed.' }
& (Join-Path $UnityEditorData 'MonoBleedingEdge/bin/mono.exe') (Join-Path $output 'Checks.exe')
if ($LASTEXITCODE -ne 0) { throw 'Logic regression checks failed.' }
