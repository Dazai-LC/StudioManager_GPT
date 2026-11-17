[CmdletBinding()]
param([switch]$Execute,[switch]$OwnerApproved)

# Windows PowerShell 5.1. Default mode inspects files only: ZERO SQL connections.
$ErrorActionPreference='Stop'
$stage='FILES'
$connection=$null
try {
    $folder=$PSScriptRoot
    if([string]::IsNullOrEmpty($folder)){$folder=Join-Path (Get-Location) 'database\demo-local'}
    $repo=[IO.Directory]::GetParent($folder).Parent.FullName
    $sqlPath=Join-Path $folder '01_BOOTSTRAP_REVIEW_ONLY.sql'
    $review=Get-Content -LiteralPath (Join-Path $folder 'source-review.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if((Get-FileHash -LiteralPath $sqlPath -Algorithm SHA256).Hash -cne $review.BootstrapSHA256){throw 'Review binding mismatch'}
    foreach($source in $review.OriginalScripts) {
        if((Get-FileHash -LiteralPath (Join-Path $repo $source.Path) -Algorithm SHA256).Hash -cne $source.SHA256){throw 'Source changed; review again'}
    }
    $sql=[IO.File]::ReadAllText($sqlPath)
    $disabled='DECLARE @OwnerApprovedLocalDemo bit=0;'
    if([regex]::Matches($sql,[regex]::Escape($disabled)).Count -ne 1 -or $sql -match '(?im)^\s*(USE|GO|CREATE DATABASE|ALTER DATABASE|DROP DATABASE)\b') {
        throw 'Unexpected bootstrap payload'
    }
    if(-not $Execute -and -not $OwnerApproved) {
        Write-Output 'LOCAL_DEMO_PREPARED; TARGET=MSI\SQLEXPRESS / StudioManager_P04_Test_20261116A; SQL_CONNECTIONS=0; EXECUTION_DISABLED'
        exit 0
    }
    $stage='APPROVAL'
    if(-not $Execute -or -not $OwnerApproved){throw 'Both explicit execution approvals are required'}
    if($PSVersionTable.PSEdition -ne 'Desktop' -or $PSVersionTable.PSVersion.Major -ne 5 -or $env:COMPUTERNAME -cne 'MSI') {
        throw 'Use Windows PowerShell 5.1 on the approved MSI machine'
    }
    # The local self-signed certificate exception is confined to this exact disposable target.
    Add-Type -AssemblyName System.Data -ErrorAction Stop
    $builder=New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder['Data Source']='MSI\SQLEXPRESS'
    $builder['Initial Catalog']='StudioManager_P04_Test_20261116A'
    $builder['Integrated Security']=$true
    $builder['Encrypt']=$true
    $builder['TrustServerCertificate']=$true
    $builder['Pooling']=$false
    $builder['Enlist']=$false
    $builder['Connect Timeout']=5
    $builder['Application Name']='StudioManager.LocalDemo.Bootstrap'
    $connection=New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
    $stage='CONNECTION'
    $connection.Open()
    $stage='BOOTSTRAP'
    $command=$connection.CreateCommand()
    $command.CommandTimeout=120
    # Do not edit/enable the repository SQL file: authorize this reviewed in-memory batch only.
    $command.CommandText=$sql.Replace($disabled,'DECLARE @OwnerApprovedLocalDemo bit=1;')
    $reader=$command.ExecuteReader()
    try {
        if(-not $reader.Read() -or $reader.GetString(0) -cne 'StudioManager_P04_Test_20261116A' -or $reader.GetInt32(1) -ne 19) {
            throw 'Unexpected completion record; inspect outcome'
        }
        Write-Output 'LOCAL_DEMO_BOOTSTRAP_COMPLETED; TABLES=19; TARGET=MSI\SQLEXPRESS / StudioManager_P04_Test_20261116A'
    } finally {$reader.Dispose();$command.Dispose()}
    exit 0
} catch {
    # Fixed diagnostics only. Do not print raw provider errors or connection material.
    Write-Output ('LOCAL_DEMO_STOPPED; STAGE='+$stage+'; REVIEW_OUTCOME_BEFORE_ANY_RETRY; NO_AUTOMATIC_RESET_OR_RETRY')
    exit 1
} finally {
    if($null -ne $connection){$connection.Dispose()}
}
