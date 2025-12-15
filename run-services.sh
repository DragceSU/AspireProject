#!/usr/bin/env bash
set -euo pipefail

PAYMENT_INSTANCES=3
RABBIT_HOST=localhost

# Simple arg parsing
while [[ $# -gt 0 ]]; do
  case "$1" in
    -p|--paymentInstances)
      PAYMENT_INSTANCES="$2"; shift 2 ;;
    -r|--rabbitHost)
      RABBIT_HOST="$2"; shift 2 ;;
    -h|--help)
      echo "Usage: $0 [-p|--paymentInstances <num>] [-r|--rabbitHost <host>]"; exit 0 ;;
    *)
      echo "Unknown option: $1"; exit 1 ;;
  esac
done

pushd "C:/Git/AspireProject/AppHost" >/dev/null
echo "Building solution..."
dotnet build AppHost.slnx
popd >/dev/null

echo "Starting InvoiceMicroservice..."
( cd "C:/Git/AspireProject/AppHost/InvoiceMicroservice" && RABBIT_HOST="$RABBIT_HOST" dotnet run ) &

echo "Starting $PAYMENT_INSTANCES PaymentMicroservice instance(s)..."
for i in $(seq 1 "$PAYMENT_INSTANCES"); do
  ( cd "C:/Git/AspireProject/AppHost/PaymentMicroservice" && RABBIT_HOST="$RABBIT_HOST" dotnet run ) &
done

echo "Services started in background. Use 'ps' to inspect or 'wait' to block until they exit."
