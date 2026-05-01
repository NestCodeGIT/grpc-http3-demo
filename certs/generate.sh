#!/usr/bin/env bash
# Generate local TLS certificates for development using mkcert
# Prerequisites: brew install mkcert (macOS) or https://github.com/FiloSottile/mkcert

set -e

CERTS_DIR="$(dirname "$0")"

command -v mkcert >/dev/null 2>&1 || { echo "mkcert not found. Install: https://github.com/FiloSottile/mkcert"; exit 1; }

mkcert -install
mkcert -cert-file "$CERTS_DIR/api.crt" -key-file "$CERTS_DIR/api.key" localhost 127.0.0.1

echo ""
echo "✓ Certificates generated:"
echo "  certs/api.crt"
echo "  certs/api.key"
echo ""
echo "These are trusted by your local browser automatically."
