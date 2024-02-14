#!/usr/bin/env bash
set -euE pipefail

K3S_VERSION=v1.28.4-k3s1

# Check if k3d cluster exists
if k3d cluster list | grep -q warehousemanager; then
    echo "Cluster already exists"

else
    echo "Creating cluster"
    k3d cluster create warehousemanager \
        --image rancher/k3s:"${K3S_VERSION/+/-}" \
        --agents 1 \
        --wait \
        -p "30000:30000@agent:*"
fi

helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo add open-telemetry https://open-telemetry.github.io/opentelemetry-helm-charts
helm repo update

# Prepare namespaces
kubectl apply -k prepare/namespaces

# Install Monitoring Stack
helm upgrade --install kube-prometheus-stack prometheus-community/kube-prometheus-stack \
    --version 55.5.0 \
    -f prepare/kube-prometheus-stack/helm-values.yaml -n monitoring

# Install OpenTelemetry Operator
helm upgrade --install opentelemetry-operator open-telemetry/opentelemetry-operator \
    --version 0.44.2 \
    -f prepare/otel-operator/helm-values.yaml -n otel

# Wait for OpenTelemetry Operator to be ready
# There seems to be a bug in the operator that causes it to not be ready immediately
# See: https://github.com/open-telemetry/opentelemetry-helm-charts/issues/845
sleep 60

# Install OpenTelemetry Collector
kubectl apply -k base

# Install Warehouse Manager
# Build image
cd .. && docker build -t otel-demo-warehouse-warehousemanager:latest . && cd deployment
# Import image to k3d cluster
k3d image import otel-demo-warehouse-warehousemanager:latest -c warehousemanager
# Deploy Warehouse Manager
kubectl apply -k app
