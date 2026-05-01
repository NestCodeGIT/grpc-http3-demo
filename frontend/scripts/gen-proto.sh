#!/usr/bin/env bash
# Generates TypeScript gRPC-Web client from .proto files
# Run: bash scripts/gen-proto.sh

set -e

PROTO_DIR="../backend/GrpcApi/Protos"
OUT_DIR="src/app/core/grpc/generated"

mkdir -p "$OUT_DIR"

# Requires: protoc, protoc-gen-js, protoc-gen-grpc-web
# Install: npm install -g protoc-gen-js protoc-gen-grpc-web
# macOS/Linux: brew install protobuf

protoc -I="$PROTO_DIR" \
  --js_out=import_style=commonjs:"$OUT_DIR" \
  --grpc-web_out=import_style=typescript,mode=grpcwebtext:"$OUT_DIR" \
  "$PROTO_DIR/task.proto"

echo "✓ Proto files generated in $OUT_DIR"
