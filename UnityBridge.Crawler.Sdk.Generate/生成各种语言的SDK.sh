#!/bin/bash

# Default API URL (adjust port if needed)
API_URL="http://localhost:5045"
OPENAPI_URL="$API_URL/openapi/v1.json"
OUTPUT_DIR="./clients"

echo "Generating API Clients from $OPENAPI_URL..."

# Ensure npx is available
if ! command -v npx &> /dev/null; then
    echo "Error: npx is not installed. Please install Node.js."
    exit 1
fi

generate_client() {
    local lang=$1
    local out_dir=$2
    shift 2
    local extra_args="$@"

    echo "Generating $lang..."
    npx @openapitools/openapi-generator-cli generate \
        -i "$OPENAPI_URL" \
        -g "$lang" \
        -o "$OUTPUT_DIR/$out_dir" \
        $extra_args
}

# --- Languages ---

# Python
generate_client python python --additional-properties=packageName=unitybridge_sdk

# Go
generate_client go go --additional-properties=packageName=unitybridge

# C#
generate_client csharp csharp --additional-properties=packageName=UnityBridge.Sdk,targetFramework=net9.0

# Java
generate_client java java --additional-properties=apiPackage=com.unitybridge.client.api,modelPackage=com.unitybridge.client.model,groupId=com.unitybridge

# Kotlin
generate_client kotlin kotlin --additional-properties=packageName=com.unitybridge.client

# PHP
generate_client php php --additional-properties=invokerPackage=UnityBridge\\Client

# Rust
generate_client rust rust --additional-properties=packageName=unitybridge-sdk

# Swift (Swift 5)
generate_client swift5 swift --additional-properties=projectName=UnityBridgeSdk

# TypeScript (Fetch)
generate_client typescript-fetch typescript-fetch --additional-properties=npmName=@unitybridge/sdk-ts

# JavaScript
generate_client javascript javascript --additional-properties=projectName=@unitybridge/sdk-js

# --- Documentation & Specs ---

# HTML Documentation
generate_client html2 docs/html

# Markdown Documentation
generate_client markdown docs/markdown

# OpenAPI JSON
generate_client openapi docs/openapi-json

# OpenAPI YAML
generate_client openapi-yaml docs/openapi-yaml

echo "Done! All clients and documents generated in $OUTPUT_DIR"
