#!/usr/bin/env bash
set -euo pipefail

# Starts InvoiceMicroservice, PaymentMicroservice, and a single WebApi.Service container in detached mode.
# Usage: ./start-docker-instances.sh [InvoiceCount <n>] [PaymentCount <n>] [WebApiEnabled <true|false>]
# Defaults are 1 instance each for invoice/payment and WebApi enabled. Containers are suffixed with "-<n>" when count > 1.

INVOICE_COUNT=1
PAYMENT_COUNT=1
WEBAPI_ENABLED=true
WEBAPI_CONTAINER_NAME="aspire-webapi"
WEBAPI_IMAGE_NAME="aspire-webapi"
WEBAPI_PORT=5088
RABBITMQ_CONTAINER_NAME="rabbitmq"
NETWORK_NAME="aspire-net"

usage() {
  cat <<USAGE
Usage: $0 [InvoiceCount <n>] [PaymentCount <n>] [WebApiEnabled <true|false>]
Example: $0 InvoiceCount 2 PaymentCount 3 WebApiEnabled false
USAGE
  exit 1
}

while [ "$#" -gt 0 ]; do
  case "$1" in
    InvoiceCount)
      shift || usage
      INVOICE_COUNT="$1"
      ;;
    PaymentCount)
      shift || usage
      PAYMENT_COUNT="$1"
      ;;
    WebApiEnabled)
      shift || usage
      WEBAPI_ENABLED="$1"
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      ;;
  esac
  shift || break
done

validate_count() {
  local label=$1
  local value=$2
  if ! [[ "$value" =~ ^[0-9]+$ ]] || [ "$value" -lt 1 ]; then
    echo "$label count must be a positive integer (got: $value)" >&2
    exit 1
  fi
}

validate_count "Invoice" "$INVOICE_COUNT"
validate_count "Payment" "$PAYMENT_COUNT"
if ! [[ "$WEBAPI_ENABLED" =~ ^(true|false)$ ]]; then
  echo "WebApiEnabled must be true or false (got: $WEBAPI_ENABLED)" >&2
  exit 1
fi

HOST_ALIAS="host.docker.internal"
HOST_GATEWAY="host-gateway"

start_instances() {
  local base_name=$1
  local count=$2
  local image=$3

  echo "Starting $count detached instance(s) of $image..."

  for i in $(seq 1 "$count"); do
    local name="$base_name"
    if [ "$count" -gt 1 ]; then
      name="${base_name}-${i}"
    fi

    if docker ps -a --format '{{.Names}}' | grep -qx "$name"; then
      echo "Stopping and removing existing container: $name"
      docker rm -f "$name" >/dev/null
    fi

    echo "Starting $name (detached with TTY)..."
    docker run -itd --rm \
      --name "$name" \
      --add-host="${HOST_ALIAS}:${HOST_GATEWAY}" \
      "$image"
  done
}

start_instances "invoice-microservice" "$INVOICE_COUNT" "invoice-microservice"
start_instances "payment-microservice" "$PAYMENT_COUNT" "payment-microservice"

ensure_network() {
  if ! docker network ls --format '{{.Name}}' | grep -qx "$NETWORK_NAME"; then
    echo "Creating Docker network: $NETWORK_NAME"
    docker network create "$NETWORK_NAME" >/dev/null
  fi
}

container_on_network() {
  local container=$1
  docker network inspect "$NETWORK_NAME" --format '{{range $id,$container := .Containers}}{{println $container.Name}}{{end}}' 2>/dev/null | grep -qx "$container"
}

ensure_rabbit_ready() {
  if ! docker ps --format '{{.Names}}' | grep -qx "$RABBITMQ_CONTAINER_NAME"; then
    echo "RabbitMQ container '$RABBITMQ_CONTAINER_NAME' must be running before starting WebApi.Service." >&2
    exit 1
  fi
}

start_webapi() {
  ensure_rabbit_ready
  ensure_network

  if ! container_on_network "$RABBITMQ_CONTAINER_NAME"; then
    echo "Attaching $RABBITMQ_CONTAINER_NAME to $NETWORK_NAME..."
    docker network connect "$NETWORK_NAME" "$RABBITMQ_CONTAINER_NAME" >/dev/null || true
  fi

  if docker ps -a --format '{{.Names}}' | grep -qx "$WEBAPI_CONTAINER_NAME"; then
    echo "Stopping and removing existing container: $WEBAPI_CONTAINER_NAME"
    docker rm -f "$WEBAPI_CONTAINER_NAME" >/dev/null
  fi

  echo "Starting $WEBAPI_CONTAINER_NAME (detached, mapped to http://localhost:${WEBAPI_PORT})..."
  docker run -d --rm \
    --name "$WEBAPI_CONTAINER_NAME" \
    --network "$NETWORK_NAME" \
    -p "${WEBAPI_PORT}:8080" \
    -e ASPNETCORE_ENVIRONMENT=Development \
    "$WEBAPI_IMAGE_NAME" >/dev/null
}

if [ "$WEBAPI_ENABLED" = "true" ]; then
  start_webapi
fi

echo "Done. Use 'docker ps' to view containers and 'docker logs -f <name>' to follow output."
