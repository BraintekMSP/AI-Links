param(
    [Parameter(Mandatory=$true)][string] $PrePath,
    [Parameter(Mandatory=$true)][string] $PostPath
)

$pre  = Get-Content $PrePath  -Raw | ConvertFrom-Json
$post = Get-Content $PostPath -Raw | ConvertFrom-Json

$preKeys  = $pre.PSObject.Properties.Name  | Sort-Object
$postKeys = $post.PSObject.Properties.Name | Sort-Object

Write-Host '--- Top-level keys (pre) ---'
$preKeys  | ForEach-Object { Write-Host "  $_" }
Write-Host '--- Top-level keys (post) ---'
$postKeys | ForEach-Object { Write-Host "  $_" }

Write-Host ''
Write-Host '--- Added keys (post only) ---'
Compare-Object $preKeys $postKeys | Where-Object { $_.SideIndicator -eq '=>' } | ForEach-Object { Write-Host "  + $($_.InputObject)" }
Write-Host '--- Removed keys (pre only) ---'
Compare-Object $preKeys $postKeys | Where-Object { $_.SideIndicator -eq '<=' } | ForEach-Object { Write-Host "  - $($_.InputObject)" }

Write-Host ''
Write-Host '--- MCP-relevant surface check ---'
Write-Host ("  pre  mcpServers present : " + ($null -ne $pre.mcpServers))
Write-Host ("  post mcpServers present : " + ($null -ne $post.mcpServers))
Write-Host ("  pre  projects present   : " + ($null -ne $pre.projects))
Write-Host ("  post projects present   : " + ($null -ne $post.projects))

if ($pre.mcpServers) {
    Write-Host "  pre  mcpServers keys    : $(($pre.mcpServers.PSObject.Properties.Name) -join ', ')"
}
if ($post.mcpServers) {
    Write-Host "  post mcpServers keys    : $(($post.mcpServers.PSObject.Properties.Name) -join ', ')"
}
