# Script para crear estructura de carpetas
Write-Host "Creando estructura de carpetas..." -ForegroundColor Green

$folders = @(
    "wwwroot/uploads",
    "wwwroot/uploads/restaurantes",
    "wwwroot/uploads/productos",
    "wwwroot/uploads/perfiles",
    "wwwroot/img"
)

foreach ($folder in $folders) {
    if (!(Test-Path $folder)) {
        New-Item -Path $folder -ItemType Directory -Force | Out-Null
        Write-Host "✓ Carpeta creada: $folder" -ForegroundColor Cyan
    } else {
        Write-Host "✓ Carpeta ya existe: $folder" -ForegroundColor Yellow
    }
}

# Crear archivos .gitkeep
$gitkeepFolders = @(
    "wwwroot/uploads",
    "wwwroot/uploads/restaurantes",
    "wwwroot/uploads/productos",
    "wwwroot/uploads/perfiles"
)

foreach ($folder in $gitkeepFolders) {
    $gitkeepPath = Join-Path $folder ".gitkeep"
    if (!(Test-Path $gitkeepPath)) {
        New-Item -Path $gitkeepPath -ItemType File -Force | Out-Null
        Write-Host "✓ Archivo .gitkeep creado en: $folder" -ForegroundColor Green
    }
}

Write-Host "`n¡Estructura de carpetas lista!" -ForegroundColor Green