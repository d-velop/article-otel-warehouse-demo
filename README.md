# Warehouse manager

This repository contains a WarehouseManager demo application written in dotnet 8 and c#.
It provides a HTTP Api to add/remove/update and query warehouse items from a local sqlite database.

Besides the WarehouseManager contained in [`./WarehouseManager`](./WarehouseManager/) it contains [kubernetes](https://kubernetes.io/) definition to deploy the app on a local Kubernetes Cluster.
These can be found in the [`./deployment`](./deployment/) folder.

## Getting started

### 1. Start the app

Run the following command:

```bash
dotnet run --project WarehouseManager/WarehouseManager.csproj
```

The app will be available at [localhost:5086](http://localhost:5086)

### 2. Local development: Start needed services

Even though the app is working without any dependencies to other services, it does not send any telemetry that we can analyze later.

To do that a few services must be up and running:

- OTel Collector
- Prometheus (for metrics)
- Grafana

This can be archived using the following command:

```bash
docker compose up -d
```

### 2. Deploy app to k8s cluster

Make sure you have the following tools installed:

- [k3d](https://k3d.io) Theoretically any k8s cluster should work, but this one is tested
- [kubectl](https://kubernetes.io/docs/tasks/tools/) The k8s cli
- [helm](https://helm.sh/) The package manager for k8s

Now simply run the following command to build the WarehouseManager image and deploy it to the cluster:

```bash
cd deployment
./deploy.sh
```

Forward ports to access services in the cluster:

```bash
kubectl port-forward services/kube-prometheus-stack-grafana 3000:80 -n monitoring
kubectl port-forward service/warehousemanager 5086:5086 -n warehousemanager
```
