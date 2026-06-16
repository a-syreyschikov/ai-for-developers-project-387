import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const tokenKey = "RENDER_MCP_TOKEN";
const rootDirectory = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const envPath = resolve(rootDirectory, ".env");

function readDotEnvValue(key) {
  if (!existsSync(envPath)) {
    return undefined;
  }

  const lines = readFileSync(envPath, "utf8").split(/\r?\n/);

  for (const line of lines) {
    const trimmed = line.trim();

    if (trimmed === "" || trimmed.startsWith("#")) {
      continue;
    }

    const assignment = trimmed.startsWith("export ")
      ? trimmed.slice("export ".length).trimStart()
      : trimmed;
    const separatorIndex = assignment.indexOf("=");

    if (separatorIndex === -1) {
      continue;
    }

    const name = assignment.slice(0, separatorIndex).trim();

    if (name !== key) {
      continue;
    }

    let value = assignment.slice(separatorIndex + 1).trim();
    const quote = value[0];

    if ((quote === '"' || quote === "'") && value.endsWith(quote)) {
      value = value.slice(1, -1);
    }

    return value;
  }

  return undefined;
}

export default async () => ({
  config(config) {
    const token = process.env[tokenKey] || readDotEnvValue(tokenKey);

    if (!token) {
      return;
    }

    process.env[tokenKey] = token;

    if (!config.mcp?.render) {
      return;
    }

    // opencode does not load project .env before MCP header interpolation.
    config.mcp.render.headers = {
      ...config.mcp.render.headers,
      Authorization: `Bearer ${token}`,
    };
  },
});
