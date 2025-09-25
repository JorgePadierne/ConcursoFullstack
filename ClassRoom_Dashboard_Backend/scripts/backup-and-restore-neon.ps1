param(
  [string]$LocalHost = "localhost",
  [string]$LocalUser = "postgres",
  [string]$LocalDb = "classroom_dashboard",
  [string]$LocalPassword = "apc5tdrd",
  [string]$NeonUri = "postgresql://neondb_owner:npg_akd6wEDoGSR4@ep-orange-flower-adw2x1bi-pooler.c-2.us-east-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require",
  [string]$OutputDir = ".\db_backups",
  [ValidateSet("sql","dump")]
  [string]$Format = "sql"
)

# Ensure output directory
if (!(Test-Path $OutputDir)) { New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null }

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

# Validate tools
function Require-Tool($name) {
  if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
    Write-Error "'$name' no está instalado o no está en PATH. Instala PostgreSQL client tools."
    exit 1
  }
}

Require-Tool pg_dump
Require-Tool psql
Require-Tool pg_restore

# Dump local database
if ($Format -eq "sql") {
  $dumpFile = Join-Path $OutputDir "${LocalDb}_$timestamp.sql"
  Write-Host "Creando dump SQL local en $dumpFile ..."
  $env:PGPASSWORD = $LocalPassword
  pg_dump -h $LocalHost -U $LocalUser -d $LocalDb -f $dumpFile
}
else {
  $dumpFile = Join-Path $OutputDir "${LocalDb}_$timestamp.dump"
  Write-Host "Creando dump en formato personalizado en $dumpFile ..."
  $env:PGPASSWORD = $LocalPassword
  pg_dump -h $LocalHost -U $LocalUser -d $LocalDb -Fc -f $dumpFile
}

if (!(Test-Path $dumpFile)) {
  Write-Error "No se creó el archivo de dump. Abortando."
  exit 1
}

# Restore to Neon
Write-Host "Restaurando en Neon: $NeonUri ..."
if ($Format -eq "sql") {
  psql "$NeonUri" -f $dumpFile
}
else {
  pg_restore --dbname="$NeonUri" --no-owner --no-privileges --clean --if-exists -v $dumpFile
}

Write-Host "Proceso completado. Verifica con: psql \"$NeonUri\" -c \"\\dt\""
