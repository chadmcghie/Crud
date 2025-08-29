# Hardware & Infrastructure Architecture Categories

## Table of Contents
- [Deployment Topologies](#deployment-topologies)
- [Hosting Environments](#hosting-environments)
- [Cloud Providers](#cloud-providers)
- [Compute Models](#compute-models)
- [Scaling](#scaling)
- [Networking](#networking)
- [Storage](#storage)
- [Resilience & Recovery](#resilience--recovery)
- [Security](#security)
- [Observability](#observability)

## Deployment Topologies
- **Monolith** → Single deployable unit, simple infra.  
- **N-Tier / Layered** → UI, business, data separated; classic enterprise setup.  
- **Microservices** → Independent deployable services; distributed by design.  
- **Serverless** → Functions as a Service (FaaS), no infra management.  

[^](#hardware--infrastructure-architecture-categories)

## Hosting Environments
- **On-Premises** → Full control, capital expense.  
- **Virtualized** → VMware, Hyper-V; abstraction of physical machines.  
- **Cloud (IaaS/PaaS/SaaS)**:  
  - **IaaS** → VMs, storage, networking (Azure VMs, AWS EC2).  
  - **PaaS** → Managed platforms (Azure App Service, AWS Elastic Beanstalk).  
  - **SaaS** → Delivered applications (M365, Salesforce).  

[^](#hardware--infrastructure-architecture-categories)

## Cloud Providers
- **Azure** → App Service, AKS, Functions, Cosmos DB, App Insights.  
- **AWS** → EC2, ECS, Lambda, RDS, DynamoDB, CloudWatch.  
- **GCP** → GKE, Cloud Run, BigQuery, Cloud SQL.  

[^](#hardware--infrastructure-architecture-categories)

## Compute Models
- **Physical Servers** → Bare metal.  
- **Virtual Machines** → Isolated OS on shared hardware.  
- **Containers** → Lightweight, portable (Docker, Podman).  
- **Orchestration** → Kubernetes (AKS, EKS, GKE).  

[^](#hardware--infrastructure-architecture-categories)

## Scaling
- **Vertical Scaling** → Add more CPU/RAM to a single node.  
- **Horizontal Scaling** → Add more nodes, load balancers.  
- **Elastic Scaling** → Auto-scale based on demand.  

[^](#hardware--infrastructure-architecture-categories)

## Networking
- **Load Balancers** → Distribute traffic across servers.  
- **APIs & Gateways** → Reverse proxies, routing, throttling.  
- **CDNs** → Content distribution for global latency reduction.  
- **VPN/VNet/Private Link** → Secure network boundaries.  

[^](#hardware--infrastructure-architecture-categories)

## Storage
- **Relational DBs** → SQL Server, Postgres, MySQL, SQLITE.  
- **NoSQL DBs** → MongoDB, Cassandra, DynamoDB, Cosmos DB.  
- **Blob / Object Storage** → S3, Azure Blob.  
- **File Systems** → NFS, Azure Files, EFS.  

[^](#hardware--infrastructure-architecture-categories)

## Resilience & Recovery
- **HA Clusters** → Active/active, active/passive.  
- **Disaster Recovery** → Backups, geo-redundancy, RTO/RPO.  
- **Redundancy** → Multi-zone, multi-region deployments.  

[^](#hardware--infrastructure-architecture-categories)

## Security
- **Identity** → OAuth2, OpenID Connect, SSO.  
- **Secrets** → Key Vault, AWS Secrets Manager.  
- **Network Security** → Firewalls, WAF, Zero Trust.  
- **Data Security** → Encryption at rest & in transit.  

[^](#hardware--infrastructure-architecture-categories)

## Observability
- **Logging** → Serilog, ELK, Cloud-native logs.  
- **Metrics** → Prometheus, App Insights, CloudWatch.  
- **Tracing** → OpenTelemetry, Jaeger, Zipkin.  

[^](#hardware--infrastructure-architecture-categories)
