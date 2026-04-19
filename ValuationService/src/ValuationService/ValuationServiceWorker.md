
## Background Processing

The worker runs continuously and waits 30 minutes between valuation cycles.

### Batch behavior

- records are processed in groups of 10
- only records needing refresh are selected
- failures are logged and do not stop the service

## API Endpoints

The service currently includes commented-out endpoints for manual control:

- `POST /run-once`
- `POST /start-continuous`
- `POST /stop`

These endpoints are not active unless enabled in `Program.cs`.

## Logging

The service logs:

- startup information
- each record being processed
- successful updates
- parsing warnings
- research failures

## Notes

- The MCP client wrapper is responsible for communicating with the Playwright MCP server.
- The current implementation is intended for research-driven valuation updates.
- Make sure the Node.js and Playwright MCP environment is configured correctly before relying on automated valuation runs.
