param(
    [Parameter(Mandatory = $true)]
    [string]$PartsFolder
)

$ErrorActionPreference = 'Stop'
$referenceCsv = Join-Path $PSScriptRoot 'mushroom_split_validation.csv'
if (-not (Test-Path -LiteralPath $referenceCsv -PathType Leaf)) {
    throw "Reference file not found: $referenceCsv"
}
if (-not (Test-Path -LiteralPath $PartsFolder -PathType Container)) {
    throw "Batch folder not found: $PartsFolder"
}

$expectedRows = Import-Csv -LiteralPath $referenceCsv
$mismatches = 0
foreach ($entry in $expectedRows) {
    $filePath = Join-Path $PartsFolder $entry.file_name
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
        Write-Host "FAIL: missing $($entry.file_name)"
        $mismatches++
        continue
    }
    $actual = (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash
    if ($actual -ine $entry.sha256) {
        Write-Host "FAIL: hash mismatch $($entry.file_name)"
        $mismatches++
    } else {
        Write-Host "PASS: $($entry.file_name)"
    }
}
if ($mismatches -gt 0) {
    Write-Error "$mismatches of $($expectedRows.Count) Mushroom batches failed verification."
    exit 1
}
Write-Host "SUCCESS: All $($expectedRows.Count) Mushroom batches exactly match the archived experimental files."
exit 0
