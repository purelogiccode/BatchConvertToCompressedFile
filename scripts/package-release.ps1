param(
    [string]$Version = "2.0.0",
    [string]$OutputDirectory = "BatchConvertToCompressedFile\bin\Release",
    [string]$StageDirectory = "artifacts\release-$Version"
)

$ErrorActionPreference = "Stop"
$rids = @("win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")
$project = "BatchConvertToCompressedFile\BatchConvertToCompressedFile.csproj"
$root = (Get-Location).Path
$stage = Join-Path $root $StageDirectory
$output = Join-Path $root $OutputDirectory

New-Item -ItemType Directory -Path $stage -Force | Out-Null
New-Item -ItemType Directory -Path $output -Force | Out-Null

foreach ($rid in $rids) {
    Write-Host "Publishing $rid (framework-dependent single-file)..."
    $publishDirectory = Join-Path $stage $rid
    if (Test-Path $publishDirectory) {
        Remove-Item -Recurse -Force $publishDirectory
    }

    dotnet publish $project -c Release -r $rid --self-contained false `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=embedded `
        -p:Version=$Version `
        -p:AssemblyVersion="$Version.0" `
        -p:FileVersion="$Version.0" `
        -p:InformationalVersion=$Version `
        -o $publishDirectory --nologo | Out-Host

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $rid"
    }

    Get-ChildItem $publishDirectory -File |
        Where-Object { $_.Extension -in ".xml", ".pdb" } |
        Remove-Item -Force

    Copy-Item LICENSE, ReadMe.md, WhatsNew.md $publishDirectory -Force

    $bundleName = "BatchConvertToCompressedFile_${Version}_${rid}"
    if ($rid.StartsWith("win-")) {
        $bundlePath = Join-Path $stage "$bundleName.zip"
        if (Test-Path $bundlePath) { Remove-Item $bundlePath -Force }
        Compress-Archive -Path (Join-Path $publishDirectory "*") -DestinationPath $bundlePath -CompressionLevel Optimal
    }
    else {
        $bundlePath = Join-Path $stage "$bundleName.tar.gz"
        if (Test-Path $bundlePath) { Remove-Item $bundlePath -Force }
        python scripts\package-tar.py $publishDirectory $bundlePath
        if ($LASTEXITCODE -ne 0) {
            throw "tar packaging failed for $rid"
        }
    }

    Copy-Item $bundlePath $output -Force
    Write-Host "Created $bundleName"
}

$bundles = Get-ChildItem $stage -File | Where-Object { $_.Name -like "BatchConvertToCompressedFile_$Version`_*" }
$checksumPath = Join-Path $stage "checksums_$Version.txt"
$checksumLines = foreach ($bundle in $bundles | Sort-Object Name) {
    $hash = (Get-FileHash $bundle.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $($bundle.Name)"
}
$checksumLines | Set-Content $checksumPath -Encoding utf8
Copy-Item $checksumPath $output -Force

Write-Host ""
Write-Host "Bundles written to ${output}:"
Get-ChildItem $output -File |
    Where-Object { $_.Name -like "BatchConvertToCompressedFile_$Version`_*" -or $_.Name -eq "checksums_$Version.txt" } |
    Select-Object Name, Length |
    Format-Table -AutoSize

Write-Host "Checksums: $checksumPath"
