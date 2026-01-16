# Logging stack (Grafana + Loki)

This folder contains a small, ARM-friendly log viewer stack intended for an Oracle free VPS (ARM64).

## Start the log viewer

From this directory:

```bash
docker compose up -d
```

- Grafana listens on `127.0.0.1:3000`
- Loki listens on `127.0.0.1:3100`

Grafana is provisioned with a Loki datasource automatically.

## Reverse proxy (example: Nginx)

Use `nginx-grafana.conf` as a starting point and point your reverse proxy at `http://127.0.0.1:3000`.

## Bot configuration

HuTao writes rolling NDJSON log files and can optionally ship logs to Loki.

Environment variables:

- `HUTAO_LOG_LEVEL`: `Verbose|Debug|Information|Warning|Error|Fatal` (defaults to `Debug` in DEBUG, otherwise `Information`)
- `HUTAO_LOG_DIR`: log directory (defaults to `<app>/logs`)
- `HUTAO_LOKI_URL`: when set, logs are also sent to Loki (example: `http://127.0.0.1:3100`)

## Why not Seq?

Seq has a great UX, but it’s commercial and can be a bad fit on ARM depending on your deployment approach. Grafana + Loki is fully OSS and has solid ARM64 support.
