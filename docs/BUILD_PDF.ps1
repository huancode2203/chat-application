# BUILD_PDF.ps1 - Gộp tất cả markdown thành PDF
# Yêu cầu: Cài đặt pandoc (https://pandoc.org/installing.html)
# Hoặc dùng VS Code extension "Markdown PDF"

$docsPath = $PSScriptRoot
$outputFile = Join-Path $docsPath "..\CHATAPPLICATION_DOCUMENTATION.md"
$pdfFile = Join-Path $docsPath "..\CHATAPPLICATION_DOCUMENTATION.pdf"

# Thứ tự các file
$files = @(
    "01_TONG_QUAN.md",
    "02_CAI_DAT.md", 
    "03_MA_HOA.md",
    "04_DATABASE.md",
    "05_SERVER.md",
    "06_CLIENT.md",
    "07_HUONG_DAN.md"
)

Write-Host "=== Building Documentation ===" -ForegroundColor Cyan

# Gộp tất cả file thành 1 markdown
$content = @"
---
title: "ChatApplication - Tài liệu Kỹ thuật Chi tiết"
author: "ChatApplication Team"
date: "2025-01-10"
geometry: margin=2.5cm
fontsize: 11pt
---

"@

foreach ($file in $files) {
    $filePath = Join-Path $docsPath $file
    if (Test-Path $filePath) {
        Write-Host "Adding: $file" -ForegroundColor Green
        $content += "`n`n"
        $content += Get-Content $filePath -Raw -Encoding UTF8
        $content += "`n`n---`n"  # Page break
    } else {
        Write-Host "Missing: $file" -ForegroundColor Red
    }
}

# Lưu file markdown gộp
$content | Out-File -FilePath $outputFile -Encoding UTF8
Write-Host "`nCreated: $outputFile" -ForegroundColor Yellow

# Thử convert sang PDF bằng pandoc
$pandocPath = Get-Command pandoc -ErrorAction SilentlyContinue
if ($pandocPath) {
    Write-Host "`nConverting to PDF with pandoc..." -ForegroundColor Cyan
    & pandoc $outputFile -o $pdfFile --pdf-engine=xelatex -V mainfont="Arial"
    if (Test-Path $pdfFile) {
        Write-Host "Created: $pdfFile" -ForegroundColor Green
    }
} else {
    Write-Host @"

=== HƯỚNG DẪN TẠO PDF ===

Cách 1: Cài Pandoc
  1. Download: https://pandoc.org/installing.html
  2. Cài đặt MiKTeX (LaTeX): https://miktex.org/download
  3. Chạy lại script này

Cách 2: Dùng VS Code
  1. Mở file: CHATAPPLICATION_DOCUMENTATION.md
  2. Cài extension: "Markdown PDF" (yzane.markdown-pdf)
  3. Ctrl+Shift+P -> "Markdown PDF: Export (pdf)"

Cách 3: Dùng Online Tool
  1. Mở file CHATAPPLICATION_DOCUMENTATION.md
  2. Copy nội dung vào: https://www.markdowntopdf.com/
  3. Download PDF

"@ -ForegroundColor Yellow
}

Write-Host "`n=== Done ===" -ForegroundColor Cyan
