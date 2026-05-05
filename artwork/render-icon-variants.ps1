param(
    [int] $IconSize = 128
)

Add-Type -AssemblyName System.Drawing

function New-RoundedRectanglePath {
    param(
        [System.Drawing.RectangleF] $Rect,
        [float] $Radius
    )

    $diameter = $Radius * 2
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc($Rect.X, $Rect.Y, $diameter, $diameter, 180, 90)
    $path.AddArc($Rect.Right - $diameter, $Rect.Y, $diameter, $diameter, 270, 90)
    $path.AddArc($Rect.Right - $diameter, $Rect.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($Rect.X, $Rect.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function Fill-RoundedRectangle {
    param(
        [System.Drawing.Graphics] $Graphics,
        [System.Drawing.Brush] $Brush,
        [System.Drawing.RectangleF] $Rect,
        [float] $Radius
    )

    $path = New-RoundedRectanglePath -Rect $Rect -Radius $Radius
    try {
        $Graphics.FillPath($Brush, $path)
    }
    finally {
        $path.Dispose()
    }
}

function Draw-RoundedRectangle {
    param(
        [System.Drawing.Graphics] $Graphics,
        [System.Drawing.Pen] $Pen,
        [System.Drawing.RectangleF] $Rect,
        [float] $Radius
    )

    $path = New-RoundedRectanglePath -Rect $Rect -Radius $Radius
    try {
        $Graphics.DrawPath($Pen, $path)
    }
    finally {
        $path.Dispose()
    }
}

function Get-OuterShellRect {
    param([int] $Size)

    return [System.Drawing.RectangleF]::new($Size * 0.055, $Size * 0.055, $Size * 0.89, $Size * 0.89)
}

function Add-Scanlines {
    param(
        [System.Drawing.Graphics] $Graphics,
        [int] $Width,
        [int] $Height,
        [System.Drawing.Color] $Color,
        [int] $Step = 6
    )

    $pen = [System.Drawing.Pen]::new($Color, 1)
    try {
        for ($y = 0; $y -lt $Height; $y += $Step) {
            $Graphics.DrawLine($pen, 0, $y, $Width, $y)
        }
    }
    finally {
        $pen.Dispose()
    }
}

function Draw-TerminalShell {
    param(
        [System.Drawing.Graphics] $Graphics,
        [int] $Size,
        [System.Drawing.Color] $Border,
        [System.Drawing.Color] $Panel,
        [System.Drawing.Color] $Header
    )

    $outer = Get-OuterShellRect -Size $Size
    $inner = [System.Drawing.RectangleF]::new($Size * 0.109, $Size * 0.117, $Size * 0.782, $Size * 0.773)
    $headerRect = [System.Drawing.RectangleF]::new($inner.X, $inner.Y, $inner.Width, $Size * 0.094)

    $outerBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.PointF]::new(0, 0),
        [System.Drawing.PointF]::new($Size, $Size),
        [System.Drawing.Color]::FromArgb(255, 18, 28, 24),
        [System.Drawing.Color]::FromArgb(255, 7, 16, 13))
    $panelBrush = [System.Drawing.SolidBrush]::new($Panel)
    $headerBrush = [System.Drawing.SolidBrush]::new($Header)
    $borderPen = [System.Drawing.Pen]::new($Border, [Math]::Max(2, $Size * 0.016))
    $glowPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(120, $Border), [Math]::Max(6, $Size * 0.04))

    try {
        Fill-RoundedRectangle -Graphics $Graphics -Brush $outerBrush -Rect $outer -Radius ($Size * 0.11)
        Fill-RoundedRectangle -Graphics $Graphics -Brush $panelBrush -Rect $inner -Radius ($Size * 0.07)
        Fill-RoundedRectangle -Graphics $Graphics -Brush $headerBrush -Rect $headerRect -Radius ($Size * 0.07)
        Draw-RoundedRectangle -Graphics $Graphics -Pen $glowPen -Rect $outer -Radius ($Size * 0.11)
        Draw-RoundedRectangle -Graphics $Graphics -Pen $borderPen -Rect $inner -Radius ($Size * 0.07)
    }
    finally {
        $outerBrush.Dispose()
        $panelBrush.Dispose()
        $headerBrush.Dispose()
        $borderPen.Dispose()
        $glowPen.Dispose()
    }

    $indicatorBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(220, 255, 190, 118))
    $indicatorDimBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(110, 255, 190, 118))
    try {
        $Graphics.FillRectangle($indicatorBrush, $Size * 0.164, $Size * 0.152, $Size * 0.07, $Size * 0.02)
        $Graphics.FillRectangle($indicatorDimBrush, $Size * 0.25, $Size * 0.152, $Size * 0.039, $Size * 0.02)
    }
    finally {
        $indicatorBrush.Dispose()
        $indicatorDimBrush.Dispose()
    }
}

function Draw-PhoneAccent {
    param(
        [System.Drawing.Graphics] $Graphics,
        [float] $X,
        [float] $Y,
        [float] $Scale,
        [System.Drawing.Color] $Color
    )

    $handsetPen = [System.Drawing.Pen]::new($Color, 11 * $Scale)
    $handsetPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $handsetPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $cupBrush = [System.Drawing.SolidBrush]::new($Color)
    $state = $Graphics.Save()

    try {
        $Graphics.DrawArc($handsetPen, $X + (3 * $Scale), $Y + (4 * $Scale), 50 * $Scale, 24 * $Scale, 195, 150)

        $Graphics.TranslateTransform($X + (10 * $Scale), $Y + (15 * $Scale))
        $Graphics.RotateTransform(-28)
        $Graphics.FillEllipse($cupBrush, -(10 * $Scale), -(6.5 * $Scale), 20 * $Scale, 13 * $Scale)
        $Graphics.Restore($state)
        $state = $Graphics.Save()

        $Graphics.TranslateTransform($X + (48 * $Scale), $Y + (15 * $Scale))
        $Graphics.RotateTransform(28)
        $Graphics.FillEllipse($cupBrush, -(10 * $Scale), -(6.5 * $Scale), 20 * $Scale, 13 * $Scale)
        $Graphics.Restore($state)
    }
    finally {
        $Graphics.Restore($state)
        $handsetPen.Dispose()
        $cupBrush.Dispose()
    }
}

function Render-PackageIcon {
    param(
        [string] $OutputPath,
        [int] $Size
    )

    $bitmap = New-Object System.Drawing.Bitmap($Size, $Size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
        $graphics.Clear([System.Drawing.Color]::Transparent)

        $clipPath = New-RoundedRectanglePath -Rect (Get-OuterShellRect -Size $Size) -Radius ($Size * 0.11)
        try {
            $graphics.SetClip($clipPath)
            Draw-TerminalShell -Graphics $graphics -Size $Size -Border ([System.Drawing.Color]::FromArgb(255, 69, 255, 164)) -Panel ([System.Drawing.Color]::FromArgb(255, 9, 18, 15)) -Header ([System.Drawing.Color]::FromArgb(255, 18, 42, 30))
            Add-Scanlines -Graphics $graphics -Width $Size -Height $Size -Color ([System.Drawing.Color]::FromArgb(22, 120, 255, 180)) -Step ([Math]::Max(4, [int]($Size * 0.023)))

            $promptFont = [System.Drawing.Font]::new('Consolas', $Size * 0.281, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
            $questionFont = [System.Drawing.Font]::new('Consolas', $Size * 0.18, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
            $promptBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 129, 255, 188))
            $cursorBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 129, 255, 188))
            $phoneScale = $Size * 0.00645
            $phoneX = ($Size / 2.0) - (29 * $phoneScale)
            $phoneY = $Size * 0.648

            try {
                $graphics.DrawString('>', $promptFont, $promptBrush, $Size * 0.117, $Size * 0.219)
                $graphics.DrawString('?', $questionFont, $promptBrush, $Size * 0.352, $Size * 0.309)
                $graphics.FillRectangle($cursorBrush, $Size * 0.477, $Size * 0.453, $Size * 0.086, $Size * 0.023)
                Draw-PhoneAccent -Graphics $graphics -X $phoneX -Y $phoneY -Scale $phoneScale -Color ([System.Drawing.Color]::FromArgb(255, 255, 176, 102))
            }
            finally {
                $promptFont.Dispose()
                $questionFont.Dispose()
                $promptBrush.Dispose()
                $cursorBrush.Dispose()
            }
        }
        finally {
            $graphics.ResetClip()
            $clipPath.Dispose()
        }

        $bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$variantsRoot = Join-Path $PSScriptRoot 'variants'
New-Item -ItemType Directory -Path $variantsRoot -Force | Out-Null

$variantPath = Join-Path $variantsRoot 'prompt-question-phone.png'
$packageIconPath = Join-Path $PSScriptRoot 'package-icon.png'

Render-PackageIcon -OutputPath $variantPath -Size 256
Render-PackageIcon -OutputPath $packageIconPath -Size $IconSize

Get-Item $variantPath, $packageIconPath | Select-Object Name, Length