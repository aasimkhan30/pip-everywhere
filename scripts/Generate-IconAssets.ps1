[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$assetsPath = Join-Path $repositoryRoot 'Assets'

function New-RoundedRectanglePath {
    param(
        [System.Drawing.RectangleF]$Rectangle,
        [float]$Radius
    )

    $diameter = $Radius * 2
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc($Rectangle.X, $Rectangle.Y, $diameter, $diameter, 180, 90)
    $path.AddArc($Rectangle.Right - $diameter, $Rectangle.Y, $diameter, $diameter, 270, 90)
    $path.AddArc(
        $Rectangle.Right - $diameter,
        $Rectangle.Bottom - $diameter,
        $diameter,
        $diameter,
        0,
        90
    )
    $path.AddArc($Rectangle.X, $Rectangle.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-AppIconBitmap {
    param([int]$Size)

    $bitmap = [System.Drawing.Bitmap]::new(
        $Size,
        $Size,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
    )
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $inset = [float]($Size * 0.035)
    $backgroundRect = [System.Drawing.RectangleF]::new(
        $inset,
        $inset,
        $Size - ($inset * 2),
        $Size - ($inset * 2)
    )
    $backgroundPath = New-RoundedRectanglePath `
        -Rectangle $backgroundRect `
        -Radius ([float]($Size * 0.22))
    $backgroundBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        $backgroundRect,
        [System.Drawing.Color]::FromArgb(255, 31, 143, 214),
        [System.Drawing.Color]::FromArgb(255, 0, 78, 140),
        45
    )
    $graphics.FillPath($backgroundBrush, $backgroundPath)

    $screenRect = [System.Drawing.RectangleF]::new(
        [float]($Size * 0.16),
        [float]($Size * 0.20),
        [float]($Size * 0.67),
        [float]($Size * 0.49)
    )
    $screenPath = New-RoundedRectanglePath `
        -Rectangle $screenRect `
        -Radius ([float]($Size * 0.075))
    $screenPen = [System.Drawing.Pen]::new(
        [System.Drawing.Color]::FromArgb(245, 255, 255, 255),
        [float]([Math]::Max(1.4, $Size * 0.055))
    )
    $screenPen.Alignment = [System.Drawing.Drawing2D.PenAlignment]::Inset
    $graphics.DrawPath($screenPen, $screenPath)

    $desktopPen = [System.Drawing.Pen]::new(
        [System.Drawing.Color]::FromArgb(190, 255, 255, 255),
        [float]([Math]::Max(1.0, $Size * 0.035))
    )
    $desktopPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $desktopPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $graphics.DrawLine(
        $desktopPen,
        [float]($Size * 0.23),
        [float]($Size * 0.78),
        [float]($Size * 0.39),
        [float]($Size * 0.78)
    )
    $graphics.DrawLine(
        $desktopPen,
        [float]($Size * 0.23),
        [float]($Size * 0.85),
        [float]($Size * 0.47),
        [float]($Size * 0.85)
    )

    $pipRect = [System.Drawing.RectangleF]::new(
        [float]($Size * 0.48),
        [float]($Size * 0.48),
        [float]($Size * 0.39),
        [float]($Size * 0.29)
    )
    $pipPath = New-RoundedRectanglePath `
        -Rectangle $pipRect `
        -Radius ([float]($Size * 0.065))
    $pipShadowPath = New-RoundedRectanglePath `
        -Rectangle ([System.Drawing.RectangleF]::new(
            $pipRect.X + ($Size * 0.025),
            $pipRect.Y + ($Size * 0.03),
            $pipRect.Width,
            $pipRect.Height
        )) `
        -Radius ([float]($Size * 0.065))
    $shadowBrush = [System.Drawing.SolidBrush]::new(
        [System.Drawing.Color]::FromArgb(75, 0, 25, 48)
    )
    $pipBrush = [System.Drawing.SolidBrush]::new(
        [System.Drawing.Color]::FromArgb(255, 244, 250, 255)
    )
    $graphics.FillPath($shadowBrush, $pipShadowPath)
    $graphics.FillPath($pipBrush, $pipPath)

    $playPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $playPath.AddPolygon([System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new($Size * 0.625, $Size * 0.555),
        [System.Drawing.PointF]::new($Size * 0.625, $Size * 0.695),
        [System.Drawing.PointF]::new($Size * 0.745, $Size * 0.625)
    ))
    $playBrush = [System.Drawing.SolidBrush]::new(
        [System.Drawing.Color]::FromArgb(255, 0, 91, 158)
    )
    $graphics.FillPath($playBrush, $playPath)

    $playBrush.Dispose()
    $playPath.Dispose()
    $pipBrush.Dispose()
    $shadowBrush.Dispose()
    $pipShadowPath.Dispose()
    $pipPath.Dispose()
    $desktopPen.Dispose()
    $screenPen.Dispose()
    $screenPath.Dispose()
    $backgroundBrush.Dispose()
    $backgroundPath.Dispose()
    $graphics.Dispose()

    return $bitmap
}

function Save-AppIconPng {
    param(
        [int]$Size,
        [string]$Path
    )

    $bitmap = New-AppIconBitmap -Size $Size
    try {
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }
}

function Save-CanvasAsset {
    param(
        [int]$Width,
        [int]$Height,
        [int]$IconSize,
        [string]$Path
    )

    $canvas = [System.Drawing.Bitmap]::new(
        $Width,
        $Height,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
    )
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $icon = New-AppIconBitmap -Size $IconSize
    try {
        $graphics.DrawImage(
            $icon,
            [int](($Width - $IconSize) / 2),
            [int](($Height - $IconSize) / 2),
            $IconSize,
            $IconSize
        )
        $canvas.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $icon.Dispose()
        $graphics.Dispose()
        $canvas.Dispose()
    }
}

function Save-MultiResolutionIco {
    param([string]$Path)

    $sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
    $images = foreach ($size in $sizes) {
        $bitmap = New-AppIconBitmap -Size $size
        $stream = [System.IO.MemoryStream]::new()
        try {
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            [pscustomobject]@{
                Size = $size
                Bytes = $stream.ToArray()
            }
        }
        finally {
            $stream.Dispose()
            $bitmap.Dispose()
        }
    }

    $fileStream = [System.IO.File]::Create($Path)
    $writer = [System.IO.BinaryWriter]::new($fileStream)
    try {
        $writer.Write([uint16]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]$images.Count)

        $offset = 6 + (16 * $images.Count)
        foreach ($image in $images) {
            $dimension = if ($image.Size -eq 256) { 0 } else { $image.Size }
            $writer.Write([byte]$dimension)
            $writer.Write([byte]$dimension)
            $writer.Write([byte]0)
            $writer.Write([byte]0)
            $writer.Write([uint16]1)
            $writer.Write([uint16]32)
            $writer.Write([uint32]$image.Bytes.Length)
            $writer.Write([uint32]$offset)
            $offset += $image.Bytes.Length
        }

        foreach ($image in $images) {
            $writer.Write($image.Bytes)
        }
    }
    finally {
        $writer.Dispose()
        $fileStream.Dispose()
    }
}

Save-MultiResolutionIco -Path (Join-Path $assetsPath 'AppIcon.ico')
Save-AppIconPng -Size 48 -Path (Join-Path $assetsPath 'LockScreenLogo.scale-200.png')
Save-AppIconPng -Size 300 -Path (Join-Path $assetsPath 'Square150x150Logo.scale-200.png')
Save-AppIconPng -Size 88 -Path (Join-Path $assetsPath 'Square44x44Logo.scale-200.png')
Save-AppIconPng -Size 24 -Path (
    Join-Path $assetsPath 'Square44x44Logo.targetsize-24_altform-unplated.png'
)
Save-AppIconPng -Size 48 -Path (
    Join-Path $assetsPath 'Square44x44Logo.targetsize-48_altform-lightunplated.png'
)
Save-AppIconPng -Size 50 -Path (Join-Path $assetsPath 'StoreLogo.png')
Save-CanvasAsset `
    -Width 1240 `
    -Height 600 `
    -IconSize 240 `
    -Path (Join-Path $assetsPath 'SplashScreen.scale-200.png')
Save-CanvasAsset `
    -Width 620 `
    -Height 300 `
    -IconSize 180 `
    -Path (Join-Path $assetsPath 'Wide310x150Logo.scale-200.png')

Write-Host 'PiP Everywhere icon assets generated.' -ForegroundColor Green
