#!/usr/bin/env bash
# Generates TypeScript gRPC-Web client from .proto files.
# Skipped silently if the generated files already exist (so CI / Docker can
# pre-stage them and avoid re-running protoc on every install).
#
# Required tools: protoc (libprotoc ≥ 3.20) and protoc-gen-grpc-web
#   Linux:  apt-get install -y protobuf-compiler && \
#           curl -L https://github.com/grpc/grpc-web/releases/download/1.5.0/protoc-gen-grpc-web-1.5.0-linux-x86_64 \
#                -o /usr/local/bin/protoc-gen-grpc-web && chmod +x /usr/local/bin/protoc-gen-grpc-web
#   macOS:  brew install protobuf protoc-gen-grpc-web

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
FRONTEND_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PROTO_DIR="$FRONTEND_DIR/../backend/GrpcApi/Protos"
OUT_DIR="$FRONTEND_DIR/src/app/core/grpc/generated"

mkdir -p "$OUT_DIR"

if ! command -v protoc >/dev/null 2>&1; then
  echo "✖ protoc not found in PATH."
  echo "  Install: see comment at top of $0"
  exit 1
fi

if ! command -v protoc-gen-grpc-web >/dev/null 2>&1; then
  echo "✖ protoc-gen-grpc-web not found in PATH."
  echo "  Install: see comment at top of $0"
  exit 1
fi

# Newer protobuf-compiler (≥ 25) no longer ships protoc-gen-js — protoc invokes
# it implicitly for --js_out. Install via: npm install -g protoc-gen-js
if ! command -v protoc-gen-js >/dev/null 2>&1; then
  echo "✖ protoc-gen-js not found in PATH."
  echo "  Install:  npm install -g protoc-gen-js"
  exit 1
fi

protoc -I="$PROTO_DIR" \
  --js_out=import_style=commonjs:"$OUT_DIR" \
  --grpc-web_out=import_style=typescript,mode=grpcwebtext:"$OUT_DIR" \
  "$PROTO_DIR/task.proto"

echo "✓ Proto files generated in $OUT_DIR"
