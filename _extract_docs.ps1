$ErrorActionPreference = 'Stop'
$files = @(Get-ChildItem -LiteralPath 'c:\Users\lenovo\Desktop\1' -Filter '*.docx')
$out = 'd:\godotproject\test-1\_doc_extract.txt'
$sb = New-Object System.Text.StringBuilder
foreach ($f in $files) {
  [void]$sb.AppendLine(('=' * 60))
  [void]$sb.AppendLine($f.Name)
  [void]$sb.AppendLine(('SIZE:' + $f.Length))
  [void]$sb.AppendLine(('=' * 60))
  $tmp = Join-Path $env:TEMP ('dx_' + [guid]::NewGuid().ToString('N'))
  New-Item -ItemType Directory -Path $tmp | Out-Null
  $zip = Join-Path $tmp 'd.zip'
  Copy-Item -LiteralPath $f.FullName -Destination $zip
  Expand-Archive -LiteralPath $zip -DestinationPath (Join-Path $tmp 'x') -Force
  $xmlPath = Join-Path $tmp 'x\word\document.xml'
  if (Test-Path -LiteralPath $xmlPath) {
    $xml = [System.IO.File]::ReadAllText($xmlPath)
    $xml = $xml -replace '</w:p>', "`n"
    $xml = [regex]::Replace($xml, '<[^>]+>', '')
    $xml = [System.Net.WebUtility]::HtmlDecode($xml)
    $lines = $xml -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
    $text = [string]::Join("`n", $lines)
    if ($text.Length -gt 10000) { $text = $text.Substring(0, 10000) + "`n...[truncated]..." }
    [void]$sb.AppendLine($text)
  } else {
    [void]$sb.AppendLine('NO document.xml')
  }
  [void]$sb.AppendLine('')
  Remove-Item -LiteralPath $tmp -Recurse -Force
}
[System.IO.File]::WriteAllText($out, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Output ('DONE ' + (Get-Item $out).Length)
