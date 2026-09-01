# DevToys.MCP

`DevToys.MCP` exposes the DevToys command line tools as a [Model Context Protocol](https://modelcontextprotocol.io)
(MCP) server over stdio, so an MCP client (an AI assistant, an IDE agent, etc.) can discover and run them.

It reuses the exact same tool model as [DevToys CLI](../DevToys.CLI): the tools ship as the separate
`DevToys.Tools` extension and are discovered through MEF at runtime. Each `ICommandLineTool` becomes one MCP
tool whose input schema is generated from its `[CommandLineOption]` properties.

## How tools are discovered

Tools are loaded from extension plugins, exactly like DevToys CLI:

- from a `Plugins` folder next to the executable (each plugin in its own sub-folder), or
- from the folder pointed to by the `EXTRAPLUGIN` environment variable.

If no plugins are found, the server starts with zero tools.

## Building

```pwsh
dotnet build src/app/dev/platforms/desktop/DevToys.MCP/DevToys.MCP.csproj -c Release
```

## Using it from an MCP client

Point your MCP client at the built executable. For example:

```jsonc
{
  "mcpServers": {
    "devtoys": {
      "command": "path/to/DevToys.MCP.exe",
      "env": {
        // Optional: load the tools from a specific extension folder.
        "EXTRAPLUGIN": "path/to/DevToys.Tools"
      }
    }
  }
}
```

The standard output stream is reserved for the MCP protocol; all logs are written to disk only.

## Tool shape

- **Name** — each tool is exposed as `devtoys_<command-name>` (for example `devtoys_base64`).
- **Input** — a JSON object generated from the tool's `[CommandLineOption]` properties. `OneOf<...>` options
  (such as "text or a file path") are exposed as a string and parsed at invocation time.
- **Output** — the text the tool writes to the console is returned as the result text, and a structured
  payload `{ "stdout": ..., "stderr": ..., "exitCode": ... }` is included as well. A non-zero exit code marks
  the result as an error.

Tool invocations are serialized: because the tools write to the process-wide console, only one runs at a time.
