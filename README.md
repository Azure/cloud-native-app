# CNCF Projects App

## Overview
Have you ever wondered what an application architecture would look like if you committed to using mostly all graduated or incubating projects from the [Cloud Native Computing Foundation](https://www.cncf.io/projects/)? This repo, the **[CNCF](https://www.cncf.io/) Projects App**, attempts to answer that question with an example expense application that is made up almost exclusively of CNCF projects. 

## CNCF Projects Application 
The CNCF Projects App is a sample expense application simulating a user submitting an expense report. The application consists of the following components:

* [Kubernetes](https://kubernetes.io/) - Container Orchestration Cluster (CNCF)
* [Rook](https://rook.io/) - Storage Management (CNCF)
* [Harbor](https://goharbor.io/) - Container Registry (CNCF)
* [NATS](https://nats.io/) - Pub/Sub Messaging (CNCF)
* [Linkerd](https://linkerd.io/) - Service Mesh (CNCF)
* [Prometheus](https://prometheus.io/) - Monitoring (CNCF)
* [Jaeger](https://www.jaegertracing.io/) - Observability/Tracing (CNCF)
* [Knative](https://knative.dev/) - Serverless
* [MySQL](https://www.mysql.com/) - Database
* [Nginx](https://www.nginx.com/) - Kubernetes Ingress Controller
* [Tekton](https://tekton.dev/) CI/CD (CD Foundation)
* [Grafana](https://grafana.com/) - Dashboard
* [SendGrid](https://sendgrid.com/) - Email Service
* [GitHub](https://github.com/) - Code Repository
* Web Front-End & Web API - [.NET Core](https://docs.microsoft.com/en-us/dotnet/core/about)

## Architecture
Below is the documented CNCF Projects App architecture for reference.

![Alt text](/images/cncf-projects-app-arc.png)

## Install
Please follow the instructions [here](notes.md) in sequence to deploy the CNCF Projects App in your environment.

Instructions for automated deployment through gitops are [here](/gitops/readme.md).

## Contributing
This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the Microsoft Open Source Code of Conduct. For more information see the Code of Conduct FAQ or contact opencode@microsoft.com with any additional questions or comments.

