# Setup

Create a Kubernetes cluster with a minimum of 3 nodes and ~8+GB per node (e.g., Standard_DS3_v2)

Fork this repository (needed to enable CD) and clone it

Install Nginx Ingress controller https://docs.microsoft.com/en-us/azure/aks/ingress-basic#create-an-ingress-controller
Following commands from the first section of the referenced Docs Link is needed. 

```
# Create a namespace for your ingress resources
kubectl create namespace ingress-basic

# Add the ingress-nginx repository
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update

# Use Helm to deploy an NGINX ingress controller
helm install nginx-ingress ingress-nginx/ingress-nginx \
    --version 3.23.0 \
    --namespace ingress-basic \
    --set controller.replicaCount=2 \
    --set controller.nodeSelector."beta\.kubernetes\.io/os"=linux \
    --set defaultBackend.nodeSelector."beta\.kubernetes\.io/os"=linux \
    --set controller.admissionWebhooks.patch.nodeSelector."beta\.kubernetes\.io/os"=linux
```
Set the variable to be used as the top level domain for this exercise. Use a custom domain or a cloud service provided domain name.
```
topLevelDomain=desiredhostnamename.com
```

If using AKS, a DNS name label can be assigend to the public IP of the Loadbalancer
 - Open the Public IP resource associated with the EXTERNAL-IP address of the LoadBalancer service
 - Navigate to the Configuration blade and set a unique name in the DNS name label
 - Use the FQDN. For ex. uniquename.centralus.cloudapp.azure.com

## Rook Installation

```
kubectl apply -f yml/rook-common.yaml
kubectl apply -f yml/rook-operator.yaml
kubectl apply -f yml/rook-cluster.yaml
kubectl apply -f yml/rook-storageclass.yaml
```

## Harbor Installation

Install Ingress for Harbor.
```
# Create namespace for the harbor nginx ingress controller 
kubectl create namespace harbor-ingress-system

# Install nginx ingress for Harbor
helm install harbor-nginx-ingress ingress-nginx/ingress-nginx \
    --version 3.23.0 \
    --namespace harbor-ingress-system \
    --set controller.ingressClass=harbor-nginx \
    --set controller.replicaCount=2 \
    --set controller.nodeSelector."beta\.kubernetes\.io/os"=linux \
    --set defaultBackend.nodeSelector."beta\.kubernetes\.io/os"=linux

# Label the harbor-ingress-system namespace to disable cert resource validation
kubectl label namespace harbor-ingress-system cert-manager.io/disable-validation=true
```

Install Cert Manager
```
# Label the ingress-basic namespace to disable resource validation
kubectl label namespace ingress-basic cert-manager.io/disable-validation=true

# Add the Jetstack Helm repository
helm repo add jetstack https://charts.jetstack.io
helm repo update

# Install the cert-manager Helm chart
helm install cert-manager jetstack/cert-manager\
  --namespace ingress-basic \
  --version v0.16.1 \
  --set installCRDs=true \
  --set nodeSelector."beta\.kubernetes\.io/os"=linux
  
```

Create the ClusterIssuer by applying the below YAML with the email address changed

```
cat <<EOF | kubectl create -f -
apiVersion: cert-manager.io/v1alpha2
kind: ClusterIssuer
metadata:
  name: letsencrypt
  namespace: ingress-basic
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: MY_EMAIL_ADDRESS
    privateKeySecretRef:
      name: letsencrypt
    solvers:
    - http01:
        ingress:
          class: harbor-nginx
          podTemplate:
            spec:
              nodeSelector:
                "kubernetes.io/os": linux      
EOF
```

Retrieve the public IP of the Loadbalancer service
```
kubectl get svc -n harbor-ingress-system
```

Assign a DNS label to the Ingress Public IP and update it for the registryHost variable

If using AKS, a DNS name label can be assigend to the public IP of the Loadbalancer created in the harbor-ingress-system namespace.
 - Open the Public IP resource associated with the EXTERNAL-IP address of the LoadBalancer service for harbor ingress
 - Navigate to the Configuration blade and set a unique name in the DNS name label
 - Use the FQDN. For ex. uniquenameforharboringress.centralus.cloudapp.azure.com

```
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
	--version 1.6.0 \
	--set expose.ingress.hosts.core=$registryHost \
	--set expose.tls.secretName=ingress-cert-harbor \
	--set notary.enabled=false \
	--set trivy.enabled=false \
	--set expose.ingress.annotations."kubernetes\.io/ingress\.class"=harbor-nginx \
	--set expose.ingress.annotations."cert-manager\.io/cluster-issuer"=letsencrypt  \
	--set persistence.enabled=true \
	--set externalURL=$externalUrl \
	--set harborAdminPassword=admin \
	--set persistence.persistentVolumeClaim.registry.storageClass=rook-ceph-block \
	--set persistence.persistentVolumeClaim.chartmuseum.storageClass=rook-ceph-block \
	--set persistence.persistentVolumeClaim.jobservice.storageClass=rook-ceph-block \
	--set persistence.persistentVolumeClaim.database.storageClass=rook-ceph-block \
	--set persistence.persistentVolumeClaim.redis.storageClass=rook-ceph-block 

```
Patch the database stateful for the Harbor database so it will not error on pod restarts
```
kubectl patch statefulset harbor-harbor-database -n harbor-system --patch "$(cat yml/harbor-init-patch.yaml)"
```
Confirm harber installed and running then create the harbor project and user
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
Use the following credentials to login:\
admin\
admin

## MySQL Installation

Deploy Mysql

```
kubectl create ns mysql

helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update

helm install mysql bitnami/mysql \
	--namespace mysql \
	--version 8.5.1 \
	--set auth.rootPassword=FTA@CNCF0n@zure3 \
	--set auth.username=ftacncf  \
	--set auth.password=FTA@CNCF0n@zure3 \
	--set global.storageClass=rook-ceph-block 
```

Create the databases 
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

## OpenFaaS

```
helm repo add openfaas https://openfaas.github.io/faas-netes/
helm repo update

kubectl apply -f https://raw.githubusercontent.com/openfaas/faas-netes/master/namespaces.yml

kubectl -n openfaas create secret generic basic-auth --from-literal=basic-auth-user=admin --from-literal=basic-auth-password="FTA@CNCF0n@zure3"

helm install openfaas openfaas/openfaas -f yml/openfaas-values.yaml -n openfaas
```

```
kubectl port-forward deploy/gateway 8080:8080 -n openfaas

Browse to http://localhost:8080 and use the username/password as admin/FTA@CNCF0n@zure3
```

Install the Nats Connector
```
kubectl apply -f yml/openfaas-nats-connector.yaml
```
## Prometheus

```
kubectl create ns monitoring
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update
helm install prometheus prometheus-community/kube-prometheus-stack -f yml/prometheus-values.yaml  \
  -n monitoring \
  --version 13.13.0
```
```
kubectl port-forward deploy/prometheus-grafana 8070:3000 -n monitoring
Browse to http://localhost:8070 and use the username/password as admin/FTA@CNCF0n@zure3

kubectl port-forward svc/prometheus-kube-prometheus-prometheus 9090:9090 -n monitoring 
Browse to http://localhost:9090
```

## Jaeger

```
helm repo add jaegertracing https://jaegertracing.github.io/helm-charts
helm repo update

kubectl create ns tracing
helm install jaeger jaegertracing/jaeger -f yml/jaeger-values.yaml \
  -n tracing \
  --version 0.40.1
```
```
# Wait for at least ~5 minutes before browsing to the Jaeger UI
kubectl port-forward svc/jaeger-query 8060:80 -n tracing
Browse to http://localhost:8060
```

## Linkerd

Deploy Linkered
```
# Install cli
curl -sL https://run.linkerd.io/install | sed s/LINKERD2_VERSION=.\*/LINKERD2_VERSION=${LINKERD2_VERSION:-stable-2.10.0}/ | sh
export PATH=$PATH:$HOME/.linkerd2/bin
linkerd version
linkerd check --pre

# Generate certificates.
wget https://github.com/smallstep/cli/releases/download/v0.15.2/step-cli_0.15.2_amd64.deb
sudo dpkg -i step-cli_0.15.2_amd64.deb

step certificate create identity.linkerd.cluster.local ca.crt ca.key --profile root-ca --no-password --insecure
step certificate create identity.linkerd.cluster.local issuer.crt issuer.key --ca ca.crt --ca-key ca.key --profile intermediate-ca --not-after 8760h --no-password --insecure

# Install linkerd
linkerd install --identity-trust-anchors-file ca.crt --identity-issuer-certificate-file issuer.crt --identity-issuer-key-file issuer.key | kubectl apply -f -

# Install linkerd dashboard
linkerd viz install | kubectl apply -f -
```

Integrate Openfaas with Linkerd (need to wait for Linker do to come up)
```
kubectl -n openfaas get deploy gateway -o yaml | linkerd inject --skip-outbound-ports=4222 - | kubectl apply -f -
```

Integrate Nginx Ingress controller with Linkerd
```
kubectl get deploy/nginx-ingress-ingress-nginx-controller -n ingress-basic -o yaml | linkerd inject - | kubectl apply -f - 
```

Linkerd metrics integration with Prometheus
```
kubectl create secret generic prometheus-kube-prometheus-prometheus-scrape-confg-linkerd --from-file=additional-scrape-configs.yaml=yml/linkerd-prometheus-additional.yaml -n monitoring

kubectl get prometheus prometheus-kube-prometheus-prometheus -n monitoring -o yaml | sed s/prometheus-kube-prometheus-prometheus-scrape-confg/prometheus-kube-prometheus-prometheus-scrape-confg-linkerd/ | kubectl apply -f -

```

Linkerd integration with Jaeger
```
linkerd jaeger install --set collector.jaegerAddr='http://jaeger-collector.tracing:14268/api/traces' | kubectl apply -f -

kubectl annotate namespace openfaas-fn config.linkerd.io/trace-collector=collector.linkerd-jaeger:55678
kubectl annotate namespace openfaas config.linkerd.io/trace-collector=collector.linkerd-jaeger:55678
kubectl annotate namespace ingress-basic config.linkerd.io/trace-collector=collector.linkerd-jaeger:55678
```

Open the dashboard in browser (linkerd-viz may take up to ~12 minutes to start)
```
linkerd viz dashboard
```

## Tekton
Install Tekton pipelines
```
kubectl apply -f https://storage.googleapis.com/tekton-releases/pipeline/previous/v0.21.0/release.yaml

kubectl apply -f yml/tekton-default-configmap.yaml  -n  tekton-pipelines
kubectl apply -f yml/tekton-pvc-configmap.yaml -n  tekton-pipelines
kubectl apply -f yml/tekton-feature-flags-configmap.yaml -n  tekton-pipelines
```
Install Tekton Triggers
```
kubectl apply --filename https://storage.googleapis.com/tekton-releases/triggers/previous/v0.13.0/release.yaml
kubectl apply --filename https://storage.googleapis.com/tekton-releases/triggers/previous/v0.13.0/interceptors.yaml
```
Install Tekton Dashboard
```
kubectl apply --filename https://github.com/tektoncd/dashboard/releases/download/v0.14.0/tekton-dashboard-release.yaml
```
```
kubectl port-forward svc/tekton-dashboard 8080:9097  -n tekton-pipelines
Browse to http://localhost:8080
```

## App Installation

Create a namespace for app deployment and annotate it for linkerd and jaeger operations
```
kubectl create ns conexp-mvp
kubectl annotate namespace conexp-mvp linkerd.io/inject=enabled
kubectl annotate namespace conexp-mvp config.linkerd.io/skip-outbound-ports="4222"
kubectl annotate namespace conexp-mvp config.linkerd.io/trace-collector=collector.linkerd-jaeger:55678
```

Create the registry credentials in the deployment namespaces
```
kubectl create secret docker-registry regcred --docker-server="https://$registryHost" --docker-username=conexp  --docker-password=FTA@CNCF0n@zure3  --docker-email=user@mycompany.com -n conexp-mvp
kubectl create secret docker-registry regcred --docker-server="https://$registryHost" --docker-username=conexp  --docker-password=FTA@CNCF0n@zure3  --docker-email=user@mycompany.com -n openfaas-fn
```

## Tekton - App Deployment

```
kubectl create ns conexp-mvp-devops
kubectl apply -f yml/tekton-limit-range.yaml

kubectl apply -f yml/app-admin-role.yaml -n conexp-mvp-devops
```

Update secret (basic-user-pass) for registry credentails, TriggerBinding for registry name, and namespaces in triggers.yaml. Create a SendGrid Account and set an API key for use. Reference this [link](https://sendgrid.com/) to create a free SendGrid account and get the SendGrid API key.
```
sendGridApiKey=<<set the api key>>
appHostName=$topLevelDomain

sed -i "s/{registryHost}/$registryHost/g" yml/app-triggers.yaml

sed -i "s/{SENDGRIDAPIKEYRELACE}/$sendGridApiKey/g" yml/app-pipeline.yaml
sed -i "s/{APPHOSTNAMEREPLACE}/$appHostName/g" yml/app-pipeline.yaml

kubectl apply -f yml/app-pipeline.yaml -n conexp-mvp-devops
kubectl apply -f yml/app-triggers.yaml -n conexp-mvp-devops
```

Roles and bindings in the deployment namespace
```
kubectl apply -f yml/app-deploy-rolebinding.yaml -n conexp-mvp
kubectl apply -f yml/app-deploy-rolebinding.yaml -n openfaas-fn
```

Expose the Tekton Event Listener externally through an Ingress for Github to dispatch the push events
```
cicdWebhookHost=$topLevelDomain

sed -i "s/{cicdWebhook}/$cicdWebhookHost/g" yml/tekton-el-ingress.yaml

kubectl apply -f yml/tekton-el-ingress.yaml -n conexp-mvp-devops

# Payload URL to be used for creating the webhook
echo https://$cicdWebhookHost/cd
```

Create a Webook in the GitHub repo of the source code by navigating to {Repo} -> Setting -> Webhook -> Add Webhook. Enter the Payload URL from above, select the Content type as **application/json** and leave the rest as defaults.

Make a change to the readme.md file and observe the deployment in Tekton dashboard.

## Launch the Application

Navigate to the FQDN of the NGINX ingress controller set up in the first step, also refered to as the *topLevelDomain* in the first step. For example **uniquename.centralus.cloudapp.azure.com**.

This will launch the application and you can proceed to create, update, delete expenses. 
