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
KAFKA_CONTAINER_NAME="kafka-kraft"
RABBITMQ_NETWORK_NAME="aspire-net"
KAFKA_NETWORK_NAME="aspireproject_kafka-net"

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
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INVOICE_CONFIG_PATH="${SCRIPT_DIR}/AppHost/InvoiceMicroservice/appsettings.docker.json"
PAYMENT_CONFIG_PATH="${SCRIPT_DIR}/AppHost/PaymentMicroservice/appsettings.docker.json"

start_instances() {
  local base_name=$1
  local count=$2
  local image=$3
  local primary_network=$4
  local secondary_network=$5
  local extra_args=${6:-}

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
      --network "$primary_network" \
      --add-host="${HOST_ALIAS}:${HOST_GATEWAY}" \
      $extra_args \
      "$image"

    if [ -n "$secondary_network" ] && [ "$secondary_network" != "$primary_network" ]; then
      docker network connect "$secondary_network" "$name" >/dev/null || true
    fi
  done
}

ensure_network() {
  local network_name=$1
  if ! docker network ls --format '{{.Name}}' | grep -qx "$network_name"; then
    echo "Creating Docker network: $network_name"
    docker network create "$network_name" >/dev/null
  fi
}

container_on_network() {
  local network_name=$1
  local container=$2
  docker network inspect "$network_name" --format '{{range $id,$container := .Containers}}{{println $container.Name}}{{end}}' 2>/dev/null | grep -qx "$container"
}

ensure_rabbit_ready() {
  if ! docker ps --format '{{.Names}}' | grep -qx "$RABBITMQ_CONTAINER_NAME"; then
    echo "RabbitMQ container '$RABBITMQ_CONTAINER_NAME' must be running before starting WebApi.Service." >&2
    exit 1
  fi
}

ensure_kafka_ready() {
  if ! docker ps --format '{{.Names}}' | grep -qx "$KAFKA_CONTAINER_NAME"; then
    echo "Kafka container '$KAFKA_CONTAINER_NAME' must be running before starting WebApi.Service." >&2
    exit 1
  fi
}

ensure_network "$RABBITMQ_NETWORK_NAME"
ensure_network "$KAFKA_NETWORK_NAME"

if ! container_on_network "$RABBITMQ_NETWORK_NAME" "$RABBITMQ_CONTAINER_NAME"; then
  echo "Attaching $RABBITMQ_CONTAINER_NAME to $RABBITMQ_NETWORK_NAME..."
  docker network connect "$RABBITMQ_NETWORK_NAME" "$RABBITMQ_CONTAINER_NAME" >/dev/null || true
fi

if ! container_on_network "$KAFKA_NETWORK_NAME" "$RABBITMQ_CONTAINER_NAME"; then
  echo "Attaching $RABBITMQ_CONTAINER_NAME to $KAFKA_NETWORK_NAME..."
  docker network connect "$KAFKA_NETWORK_NAME" "$RABBITMQ_CONTAINER_NAME" >/dev/null || true
fi

INVOICE_EXTRA_ARGS="-v ${INVOICE_CONFIG_PATH}:/app/appsettings.json:ro"
PAYMENT_EXTRA_ARGS="-v ${PAYMENT_CONFIG_PATH}:/app/appsettings.json:ro"

start_instances "invoice-microservice" "$INVOICE_COUNT" "invoice-microservice" "$RABBITMQ_NETWORK_NAME" "$KAFKA_NETWORK_NAME" "$INVOICE_EXTRA_ARGS"
start_instances "payment-microservice" "$PAYMENT_COUNT" "payment-microservice" "$RABBITMQ_NETWORK_NAME" "$KAFKA_NETWORK_NAME" "$PAYMENT_EXTRA_ARGS"

start_webapi() {
  ensure_rabbit_ready
  ensure_kafka_ready

  if docker ps -a --format '{{.Names}}' | grep -qx "$WEBAPI_CONTAINER_NAME"; then
    echo "Stopping and removing existing container: $WEBAPI_CONTAINER_NAME"
    docker rm -f "$WEBAPI_CONTAINER_NAME" >/dev/null
  fi

  echo "Starting $WEBAPI_CONTAINER_NAME (detached, mapped to http://localhost:${WEBAPI_PORT})..."
  docker run -d --rm \
    --name "$WEBAPI_CONTAINER_NAME" \
    --network "$RABBITMQ_NETWORK_NAME" \
    -p "${WEBAPI_PORT}:${WEBAPI_PORT}" \
    -e ASPNETCORE_ENVIRONMENT=Development \
    "$WEBAPI_IMAGE_NAME" >/dev/null

  if [ "$KAFKA_NETWORK_NAME" != "$RABBITMQ_NETWORK_NAME" ]; then
    docker network connect "$KAFKA_NETWORK_NAME" "$WEBAPI_CONTAINER_NAME" >/dev/null || true
  fi
}

if [ "$WEBAPI_ENABLED" = "true" ]; then
  start_webapi
fi

echo "Done. Use 'docker ps' to view containers and 'docker logs -f <name>' to follow output."
