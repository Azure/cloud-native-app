# Setup

Create a Kubernetes cluster with a minimum of 3 nodes and ~8+GB per node (e.g., Standard_DS3_v2)

Fork this repository (needed to enable CD) and clone it

## Nginx Ingress controller Installation

```bash
# Create a namespace for your ingress resources
kubectl create namespace ingress-basic

# Add the ingress-nginx repository
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update

# Use Helm to deploy an NGINX ingress controller
helm install nginx-ingress ingress-nginx/ingress-nginx \
--version 4.7.1 \
--namespace ingress-basic \
--set controller.replicaCount=2 \
--set controller.electionID=ingress-controller-leader \
--set controller.ingressClassResource.name=nginx \
--set controller.ingressClassResource.enabled=true \
--set controller.ingressClassResource.default=true \
--set controller.ingressClassResource.controllerValue=k8s.io/nginx \
--set controller.ingressClass=nginx \
--set controller.nodeSelector."kubernetes\.io/os"=linux \
--set defaultBackend.nodeSelector."kubernetes\.io/os"=linux \
--set controller.admissionWebhooks.patch.nodeSelector."kubernetes\.io/os"=linux \
--set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-health-probe-request-path"="/healthz" 
```

Retrieve the public IP of the Loadbalancer service

```bash
kubectl get svc -n ingress-basic
```

Assign a DNS label to the Ingress Public IP and update it for the registryHost variable

Set the variable to be used as the top level domain for this exercise. Use a custom domain or a cloud service provided domain name.

If using AKS, a DNS name label can be assigned to the public IP of the Loadbalancer

- Open the Public IP resource associated with the EXTERNAL-IP address of the LoadBalancer service
- Navigate to the Configuration blade and set a unique name in the DNS name label
- Use the FQDN. For ex. {uniquename}.{region}.cloudapp.azure.com

```bash
topLevelDomain={FQDN DNS label Name to be updated here}
```

## Cert Manager Installation

```bash
# Create namespace for cert manager
kubectl create namespace cert-manager
kubectl label namespace cert-manager cert-manager.io/disable-validation=true

# Add the Jetstack Helm repository
helm repo add jetstack https://charts.jetstack.io
helm repo update

# Install the cert-manager Helm chart
helm install cert-manager jetstack/cert-manager\
  --namespace cert-manager \
  --version v1.12.1 \
  --set installCRDs=true \
  --set nodeSelector."kubernetes\.io/os"=linux
```

Create the ClusterIssuer by applying the below YAML with the ***email address*** changed

```bash

cat <<EOF | kubectl create -f -
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt
  namespace: cert-manager
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: MY_EMAIL_ADDRESS
    privateKeySecretRef:
      name: letsencrypt
    solvers:
    - http01:
        ingress:
          class: nginx
          podTemplate:
            spec:
              nodeSelector:
                "kubernetes.io/os": linux      
EOF
```

## Rook Installation

```bash
# Create a namespace for rook
kubectl create namespace rook-ceph

# Add the rook helm repo
helm repo add rook-release https://charts.rook.io/release
helm repo update

# Use Helm to deploy an rook
helm install rook-ceph rook-release/rook-ceph \
    --version 1.11.9 \
    --namespace rook-ceph \
    -f yml/rook-values.yaml    

kubectl apply -f yml/rook-cluster.yaml 
kubectl apply -f yml/rook-storageclass.yaml
```

Wait for the installation to complete by watching the pods in the rook-ceph namespace

```bash
kubectl get po -n rook-ceph -A -w
```

## Harbor Installation

Install Ingress for Harbor.

```bash
# Create namespace for the harbor nginx ingress controller 
kubectl create namespace harbor-ingress-system

# Install nginx ingress for Harbor
helm install harbor-nginx-ingress ingress-nginx/ingress-nginx \
    --version 4.7.1 \
    --namespace harbor-ingress-system \
    --set controller.replicaCount=2 \
    --set controller.electionID=harbor-ingress-controller-leader \
    --set controller.ingressClassResource.name=harbor \
    --set controller.ingressClassResource.enabled=true \
    --set controller.ingressClassResource.default=true \
    --set controller.ingressClassResource.controllerValue=k8s.io/harbor \
    --set controller.ingressClass=harbor \
    --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-health-probe-request-path"="/healthz" \
    --set controller.nodeSelector."kubernetes\.io/os"=linux \
    --set defaultBackend.nodeSelector."kubernetes\.io/os"=linux \
    --set controller.admissionWebhooks.patch.nodeSelector."kubernetes\.io/os"=linux

# Label the harbor-ingress-system namespace to disable cert resource validation
kubectl label namespace harbor-ingress-system cert-manager.io/disable-validation=true
```

Retrieve the public IP of the Loadbalancer service

```bash
kubectl get svc -n harbor-ingress-system
```

Assign a DNS label to the Ingress Public IP and update it for the registryHost variable

If using AKS, a DNS name label can be assigned to the public IP of the Loadbalancer created in the harbor-ingress-system namespace.

- Open the Public IP resource associated with the EXTERNAL-IP address of the LoadBalancer service for harbor ingress
- Navigate to the Configuration blade and set a unique name in the DNS name label
- Use the FQDN. For ex. {uniquenameforharboringress}.{region}.cloudapp.azure.com

```bash
registryHost={FQDN DNS label Name to be updated here}
externalUrl=https://$registryHost

# Create the namespace for harbor installation
kubectl create namespace harbor-system
# Add the harbor helm repo 
helm repo add harbor https://helm.goharbor.io
helm repo update

# Install Harbor
helm install harbor harbor/harbor \
    --namespace harbor-system \
    --version 1.12.2 \
    --set expose.tls.certSource=secret \
    --set expose.tls.secret.secretName=ingress-cert-harbor \
    --set expose.ingress.hosts.core=$registryHost \
    --set expose.ingress.annotations."cert-manager\.io/cluster-issuer"=letsencrypt  \
    --set expose.ingress.annotations."acme\.cert-manager\.io/http01-ingress-class"=harbor \
    --set expose.ingress.className=harbor \
    --set notary.enabled=false \
    --set trivy.enabled=false \
    --set externalURL=$externalUrl \
    --set harborAdminPassword=admin \
    --set persistence.enabled=true \
    --set persistence.persistentVolumeClaim.registry.storageClass=rook-ceph-block \
    --set persistence.persistentVolumeClaim.chartmuseum.storageClass=rook-ceph-block \
    --set persistence.persistentVolumeClaim.jobservice.storageClass=rook-ceph-block \
    --set persistence.persistentVolumeClaim.database.storageClass=rook-ceph-block \
    --set persistence.persistentVolumeClaim.redis.storageClass=rook-ceph-block 

```

Confirm harbor installed and running then create the harbor project and user

```bash
#Create conexp project in Harbor
 curl -u admin:admin -i -k -X POST "$externalUrl/api/v2.0/projects" \
      -d "@json/harbor-project.json" \
      -H "Content-Type: application/json"

#Create conexp user in Harbor
 curl -u admin:admin -i -k -X POST "$externalUrl/api/v2.0/users" \
      -d "@json/harbor-project-user.json" \
      -H "Content-Type: application/json"

#Add the conexp user to the conexp project in Harbor

conexpid=$(curl -u admin:admin -k -s -X GET "$externalUrl/api/v2.0/projects?name=conexp" | jq '.[0].project_id')
echo "project_id: $conexpid"

 curl -u admin:admin -i -k -X POST "$externalUrl/api/v2.0/projects/$conexpid/members" \
      -d "@json/harbor-project-member.json" \
      -H "Content-Type: application/json"
```

Now retrieve the Harbor Registry URL:

```bash
echo $externalUrl
```

Use the following credentials to login:
admin\
admin

## MySQL Installation

Deploy Mysql

```bash
kubectl create ns mysql

helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update

helm install mysql bitnami/mysql \
    --namespace mysql \
    --version 9.10.5 \
    --set auth.rootPassword=FTA@CNCF0n@zure3 \
    --set auth.username=ftacncf  \
    --set auth.password=FTA@CNCF0n@zure3 \
    --set global.storageClass=rook-ceph-block 
```

Wait for the mysql instance to be ready and create the databases

```bash
kubectl run -n mysql -i -t ubuntu --image=ubuntu:18.04 --restart=Never -- bash -il
apt-get update && apt-get install mysql-client -y
mysql -h mysql --password=FTA@CNCF0n@zure3
show databases;

CREATE DATABASE conexpweb;

CREATE DATABASE conexpapi;
USE conexpapi;

CREATE TABLE CostCenters(
   CostCenterId int(11)  NOT NULL,
   SubmitterEmail text NOT NULL,
   ApproverEmail text NOT NULL,
   CostCenterName text NOT NULL,
   PRIMARY KEY ( CostCenterId )
);

INSERT INTO CostCenters (CostCenterId, SubmitterEmail,ApproverEmail,CostCenterName)  values (1, 'user1@mycompany.com', 'user1@mycompany.com','123E42');
INSERT INTO CostCenters (CostCenterId, SubmitterEmail,ApproverEmail,CostCenterName)  values (2, 'user2@mycompany.com', 'user2@mycompany.com','456C14');
INSERT INTO CostCenters (CostCenterId, SubmitterEmail,ApproverEmail,CostCenterName)  values (3, 'user3@mycompany.com', 'user3@mycompany.com','456C14');

USE conexpapi;
GRANT ALL PRIVILEGES ON *.* TO 'ftacncf'@'%';

USE conexpweb;
GRANT ALL PRIVILEGES ON *.* TO 'ftacncf'@'%';

# Exit from mysql cli
exit;

# Exit from the pod
exit;
```

## Knative Installation

```bash
# Install the Knative Serving component
kubectl apply -f https://github.com/knative/serving/releases/download/knative-v1.10.2/serving-crds.yaml
kubectl apply -f https://github.com/knative/serving/releases/download/knative-v1.10.2/serving-core.yaml

# Install a networking layer
kubectl apply -f https://github.com/knative/net-kourier/releases/download/knative-v1.10.0/kourier.yaml

# Configure Kourier Networking
kubectl patch configmap/config-network \
  --namespace knative-serving \
  --type merge \
  --patch '{"data":{"ingress-class":"kourier.ingress.networking.knative.dev"}}'

# Configure the No DNS
kubectl patch configmap/config-domain \
      --namespace knative-serving \
      --type merge \
      --patch '{"data":{"example.com":""}}'

# Install Eventing
kubectl apply -f https://github.com/knative/eventing/releases/download/knative-v1.10.1/eventing-crds.yaml
kubectl apply -f https://github.com/knative/eventing/releases/download/knative-v1.10.1/eventing-core.yaml

# Install Channel and Broker
kubectl apply -f https://github.com/knative/eventing/releases/download/knative-v1.10.1/in-memory-channel.yaml
kubectl apply -f https://github.com/knative/eventing/releases/download/knative-v1.10.1/mt-channel-broker.yaml

```

## Prometheus Installation

```bash
kubectl create ns monitoring
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update
helm install prometheus prometheus-community/kube-prometheus-stack -f yml/prometheus-values.yaml  \
  -n monitoring \
  --version 47.3.0
```

```bash
kubectl port-forward deploy/prometheus-grafana 8070:3000 -n monitoring
Browse to http://localhost:8070 and use the username/password as admin/FTA@CNCF0n@zure3

kubectl port-forward svc/prometheus-kube-prometheus-prometheus 9090:9090 -n monitoring 
Browse to http://localhost:9090
```

## Jaeger Installation

```bash
helm repo add jaegertracing https://jaegertracing.github.io/helm-charts
helm repo update

kubectl create ns tracing
helm install jaeger jaegertracing/jaeger -f yml/jaeger-values.yaml \
  -n tracing \
  --version 0.71.8
```

```bash
# Wait for at least ~5 minutes before browsing to the Jaeger UI
kubectl port-forward svc/jaeger-query 8060:80 -n tracing
Browse to http://localhost:8060
```

## Linkerd Installation

Deploy Linkered

```bash
# Install cli
curl --tlsv1.2 -sSfL https://run.linkerd.io/install | sed s/LINKERD2_VERSION=.\*/LINKERD2_VERSION=${LINKERD2_VERSION:-stable-2.13.5}/ | sh
export PATH=$PATH:$HOME/.linkerd2/bin
linkerd version
linkerd check --pre

# Generate certificates.
wget https://github.com/smallstep/cli/releases/download/v0.23.4/step-cli_0.23.4_amd64.deb
sudo dpkg -i step-cli_0.23.4_amd64.deb

step certificate create identity.linkerd.cluster.local ca.crt ca.key --profile root-ca --no-password --insecure
step certificate create identity.linkerd.cluster.local issuer.crt issuer.key --ca ca.crt --ca-key ca.key --profile intermediate-ca --not-after 8760h --no-password --insecure

# Install linkerd CRDs
linkerd install --crds | kubectl apply -f -

# Install linkerd
linkerd install --identity-trust-anchors-file ca.crt --identity-issuer-certificate-file issuer.crt --identity-issuer-key-file issuer.key | kubectl apply -f -

# Install linkerd dashboard
linkerd viz install | kubectl apply -f -
```

Integrate Knative with Linkerd (need to wait for Linker do to come up)

```bash
#TODO - Linkerd/Jaeger injection 
```

Integrate Nginx Ingress controller with Linkerd

```bash
kubectl get deploy/nginx-ingress-ingress-nginx-controller -n ingress-basic -o yaml | linkerd inject - | kubectl apply -f - 
```

Linkerd metrics integration with Prometheus

```bash
kubectl create secret generic prometheus-kube-prometheus-prometheus-scrape-confg-linkerd --from-file=additional-scrape-configs.yaml=yml/linkerd-prometheus-additional.yaml -n monitoring

kubectl get prometheus prometheus-kube-prometheus-prometheus -n monitoring -o yaml | sed s/prometheus-kube-prometheus-prometheus-scrape-confg/prometheus-kube-prometheus-prometheus-scrape-confg-linkerd/ | kubectl apply -f -

```

Linkerd integration with Jaeger

```bash
linkerd jaeger install --set collector.jaegerAddr='http://jaeger-collector.tracing:14268/api/traces' | kubectl apply -f -

kubectl annotate namespace ingress-basic config.linkerd.io/trace-collector=collector.linkerd-jaeger:55678
```

Open the dashboard in browser (linkerd-viz may take up to ~12 minutes to start)

```bash
linkerd viz dashboard
```

## Tekton Installation

```bash
# Install Tekton pipelines
kubectl apply -f https://storage.googleapis.com/tekton-releases/pipeline/previous/v0.49.0/release.yaml

kubectl apply -f yml/tekton-default-configmap.yaml  -n  tekton-pipelines
kubectl apply -f yml/tekton-feature-flags-configmap.yaml -n  tekton-pipelines

# Install Tekton Triggers

kubectl apply -f https://storage.googleapis.com/tekton-releases/triggers/previous/v0.24.0/release.yaml
kubectl apply -f https://storage.googleapis.com/tekton-releases/triggers/previous/v0.24.0/interceptors.yaml

# Install Tekton Dashboard

kubectl apply -f https://storage.googleapis.com/tekton-releases/dashboard/previous/v0.37.0/release.yaml

kubectl port-forward svc/tekton-dashboard 8080:9097  -n tekton-pipelines
```

Browse to http://localhost:8080

## Prepare for App Installation

Create a namespace for app deployment and annotate it for linkerd and jaeger operations

```bash
kubectl create ns conexp-mvp
kubectl annotate namespace conexp-mvp linkerd.io/inject=enabled
kubectl annotate namespace conexp-mvp config.linkerd.io/skip-outbound-ports="4222"
kubectl annotate namespace conexp-mvp config.linkerd.io/trace-collector=collector.linkerd-jaeger:55678

# Create namespace for function deployment by knative
kubectl create ns conexp-mvp-fn
#TODO - Linkerd/Jaeger injection    
```

Create the registry credentials in the deployment namespaces

```bash
kubectl create secret docker-registry regcred --docker-server="https://$registryHost" --docker-username=conexp  --docker-password=FTA@CNCF0n@zure3  --docker-email=user@mycompany.com -n conexp-mvp
kubectl create secret docker-registry regcred --docker-server="https://$registryHost" --docker-username=conexp  --docker-password=FTA@CNCF0n@zure3  --docker-email=user@mycompany.com -n conexp-mvp-fn
```

## Tekton - App Pipelines Deployment

```bash
kubectl create ns conexp-mvp-devops
kubectl apply -f yml/tekton-limit-range.yaml

kubectl apply -f yml/app-admin-role.yaml -n conexp-mvp-devops
```

Create the docker secret for tekton pipelines to push images to the registry

```bash
CONFIG="\
{\n
    \"auths\": {\n
        \"${registryHost}\": {\n
            \"username\": \"conexp\",\n
            \"password\": \"FTA@CNCF0n@zure3\",\n
            \"email\": \"user@mycompany.com\",\n
            \"auth\": \"Y29uZXhwOkZUQUBDTkNGMG5AenVyZTM=\"\n
        }\n
    }\n
}\n"

printf "${CONFIG}" > config.json
kubectl create secret generic regcred --from-file=config.json=config.json -n conexp-mvp-devops

```

Update TriggerBinding for registry name in app-triggers.yaml. Create a SendGrid Account and set an API key for use. Reference this [link](https://sendgrid.com/) to create a free SendGrid account and get the SendGrid API key.

```bash
sendGridApiKey=<<set the api key>>
appHostName=$topLevelDomain

sed -i "s/{registryHost}/$registryHost/g" yml/app-triggers.yaml

sed -i "s/{SENDGRIDAPIKEYRELACE}/$sendGridApiKey/g" yml/app-pipeline.yaml
sed -i "s/{APPHOSTNAMEREPLACE}/$appHostName/g" yml/app-pipeline.yaml

kubectl apply -f yml/app-pipeline.yaml -n conexp-mvp-devops
kubectl apply -f yml/app-triggers.yaml -n conexp-mvp-devops
```

Roles and bindings in the deployment namespace

```bash
kubectl apply -f https://raw.githubusercontent.com/tektoncd/catalog/main/task/kaniko/0.6/kaniko.yaml -n conexp-mvp-devops
kubectl apply -f yml/git-clone.yaml -n conexp-mvp-devops
kubectl apply -f yml/app-deploy-rolebinding.yaml -n conexp-mvp
kubectl apply -f yml/app-deploy-rolebinding.yaml -n conexp-mvp-fn
```

Expose the Tekton Event Listener externally through an Ingress for Github to dispatch the push events

```bash
cicdWebhookHost=$topLevelDomain

sed -i "s/{cicdWebhook}/$cicdWebhookHost/g" yml/tekton-el-ingress.yaml

kubectl apply -f yml/tekton-el-ingress.yaml -n conexp-mvp-devops

# Payload URL to be used for creating the webhook
echo https://$cicdWebhookHost/cd
```

Create a Webook in the GitHub repo of the source code by navigating to {Repo} -> Setting -> Webhook -> Add Webhook. Enter the Payload URL from above, select the Content type as **application/json** and leave the rest as defaults.

Make a change to the readme.md file and observe the deployment in Tekton dashboard.

## Launch the Application

Navigate to the FQDN of the NGINX ingress controller set up in the first step, also refered to as the *topLevelDomain* in the first step. For example **{uniquename}.{region}.cloudapp.azure.com**.

This will launch the application and you can proceed to create, update, delete expenses.
