# Anarchy-AI Privacy

Anarchy-AI is a local runtime harness delivered through this repository.

Current delivery scope:

- Windows-first packaged runtime
- local MCP launch
- local workspace inspection and reconciliation

The plugin itself does not add a separate cloud telemetry service or plugin-specific account system.

Data handling currently follows the host surface and local workspace:

- the local MCP runtime reads workspace files needed for the tool call
- the runtime may write non-destructive reconciliation outputs when the user requests them
- any network behavior outside that local runtime is governed by the host surface that launched it

This repository does not currently publish a separate standalone privacy policy for Anarchy-AI beyond this local-delivery notice.

Use remains subject to the repository notice and license:

- `../../NOTICE`
- `../../LICENSE`
