<#
.SYNOPSIS
Compatibility wrapper for the current setup build-and-publish helper.
.DESCRIPTION
Keeps older operator and agent references working while routing every invocation to
`build-self-contained-exe.ps1`, which owns the current publish, generation, and audit flow.
.PARAMETER args
Any arguments intended for `build-self-contained-exe.ps1`.
.OUTPUTS
The same stdout, stderr, exit code, and JSON build result produced by `build-self-contained-exe.ps1`.
.NOTES
Critical dependencies: `build-self-contained-exe.ps1`, the current setup publish contract, and stable wrapper forwarding.
#>
& (Join-Path $PSScriptRoot 'build-self-contained-exe.ps1') @args
