#!/usr/bin/env bash
set -euo pipefail

# Starts InvoiceMicroservice and PaymentMicroservice containers in detached mode.
# Usage: ./start-docker-instances.sh [InvoiceCount <n>] [PaymentCount <n>]
# Defaults are 1 instance each. Containers are suffixed with "-<n>" when count > 1.

INVOICE_COUNT=1
PAYMENT_COUNT=1

usage() {
  cat <<USAGE
Usage: $0 [InvoiceCount <n>] [PaymentCount <n>]
Example: $0 InvoiceCount 2 PaymentCount 3
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

echo "Done. Use 'docker ps' to view containers and 'docker logs -f <name>' to follow output."
