
Refer the [yml](yml) folder for the yamls

- [Please find Source Code here](src)

#Setup

Create a Kubernetes Cluster

Install Nginx Ingress controller https://docs.microsoft.com/en-us/azure/aks/ingress-basic#create-an-ingress-controller
Following commands from the first section of the referenced Docs Link is needed. 

```
# Create a namespace for your ingress resources
kubectl create namespace ingress-basic

# Add the official stable repository
helm repo add stable https://kubernetes-charts.storage.googleapis.com/

# Use Helm to deploy an NGINX ingress controller
helm install nginx-ingress stable/nginx-ingress \
    --namespace ingress-basic \
    --set controller.replicaCount=2 \
    --set controller.nodeSelector."beta\.kubernetes\.io/os"=linux \
    --set defaultBackend.nodeSelector."beta\.kubernetes\.io/os"=linux
```

Set the variable to be used as the top level domain for this exercise. Use a custom domain or a cloud service provided domain name.
```
topLevelDomain=desiredhostnamename.com
```

If using AKS, a DNS name label can be assigend to the public IP of the Loadbalancer
 - Open the Public IP resource associated with the EXTERNAL-IP address of the LoadBalancer service
 - Navigate to the Configuration blade and set a unique name in the DNS name label
 - Use the FQDN. For ex. uniquename.centralus.cloudapp.azure.com

##Rook Installation

```
kubectl apply -f yml/rook-common.yaml
kubectl apply -f yml/rook-operator.yaml
kubectl apply -f yml/rook-cluster.yaml
kubectl apply -f yml/rook-storageclass.yaml
```

##Harbor Installation

Install Ingress for Harbor.
```
# Create namespace for the harbor nginx ingress controller 
kubectl create namespace harbor-ingress-system

# Add the nginx helm repo
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx

# Install nginx ingress for Harbor
helm install harbor-nginx-ingress ingress-nginx/ingress-nginx \
    --namespace harbor-ingress-system \
    --set controller.ingressClass=harbor-nginx \
    --set controller.replicaCount=2 \
    --set controller.nodeSelector."beta\.kubernetes\.io/os"=linux \
    --set defaultBackend.nodeSelector."beta\.kubernetes\.io/os"=linux

# Label the ingress-basic namespace to disable cert resource validation
kubectl label namespace harbor-ingress-system cert-manager.io/disable-validation=true
```

Install Cert Manager
```
# Label the ingress-basic namespace to disable resource validation
kubectl label namespace ingress-basic cert-manager.io/disable-validation=true

# Add the Jetstack Helm repository
helm repo add jetstack https://charts.jetstack.io

# Update your local Helm chart repository cache
helm repo update

# Install the cert-manager Helm chart
helm install \
  cert-manager \
  --namespace ingress-basic \
  --version v0.16.1 \
  --set installCRDs=true \
  --set nodeSelector."beta\.kubernetes\.io/os"=linux \
  jetstack/cert-manager
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

# Install Harbor
helm install harbor harbor/harbor \
	--namespace harbor-system \
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
Update the stateful set permission for the Harbor database so that it will not error on pod restarts
```
kubectl edit statefulset harbor-harbor-database  -n harbor-system
```

Update the command on *initContainers* to

```
chown -R postgres:postgres /var/lib/postgresql/data; chmod 700 -R /var/lib/postgresql/data
```

```
Now retrieve the Harbor Registry URL: echo $externalUrl
And use the following credentials:
admin
admin
```

##MySQL Installation

Deploy Mysql
```
kubectl create ns mysql
helm install mysql stable/mysql  --set mysqlRootPassword=FTA@CNCF0n@zure3,mysqlUser=ftacncf,mysqlPassword=FTA@CNCF0n@zure3,mysqlDatabase=conexp-mysql,persistence.storageClass=rook-ceph-block -n mysql
```
Create the databases 
```
kubectl run -n mysql -i --tty ubuntu --image=ubuntu:16.04 --restart=Never -- bash -il
apt-get update && apt-get install mysql-client -y
mysql -h mysql -p
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
```

## Vitess Installation (Note: Either do the SQL Installation as above or Vitess, not both)

Clone the Vitess Git repo
```
sudo git clone https://github.com/vitessio/vitess.git 
```
Navigate to the following folder:
```
~/Vitess/vitess/examples/operator
```
Run the following kubectl commands:
```
kubectl apply -f operator.yaml 

kubectl apply -f 101_initial_cluster.yaml
```
Get the POD running the Vitess (from the pf.sh file):
```
kubectl get deployment --selector="planetscale.com/component=vtgate"
```
Expose the deploy as a svc, get the service YAML, and edit it/clean it
To Do: Add the example YAML file.

Install MySQL Client Locally
```
apt install mysql-client
```

##OpenFaaS

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
##Prometheus

```
kubectl create ns monitoring
helm repo add stable https://kubernetes-charts.storage.googleapis.com
helm install prometheus stable/prometheus-operator -f yml/prometheus-values.yaml -n monitoring
```
```
kubectl port-forward deploy/prometheus-grafana 8080:3000 -n monitoring
Browse to http://localhost:8080 and use the username/password as admin/FTA@CNCF0n@zure3

kubectl port-forward svc/prometheus-prometheus-oper-prometheus 9090:9090 -n monitoring
Browse to http://localhost:9090
```

##Jaeger

```
helm repo add jaegertracing https://jaegertracing.github.io/helm-charts
helm repo update

kubectl create ns tracing
helm install jaeger jaegertracing/jaeger -f yml/jaeger-values.yaml -n tracing
```
```
# Wait for at least ~5 minutes before browsing to the Jaeger UI
kubectl port-forward svc/jaeger-query 8080:80 -n tracing
Browse to http://localhost:8080
```

##Linkerd

Deploy Linkered
```
# Install cli
curl -sL https://run.linkerd.io/install | sh
export PATH=$PATH:$HOME/.linkerd2/bin
linkerd version
linkerd check --pre

# Generate certificates.
wget https://github.com/smallstep/cli/releases/download/v0.15.2/step-cli_0.15.2_amd64.deb
​sudo dpkg -i step-cli_0.15.2_amd64.deb

step certificate create identity.linkerd.cluster.local ca.crt ca.key --profile root-ca --no-password --insecure
step certificate create identity.linkerd.cluster.local issuer.crt issuer.key --ca ca.crt --ca-key ca.key --profile intermediate-ca --not-after 8760h --no-password --insecure

# Install linkerd
linkerd install --identity-trust-anchors-file ca.crt --identity-issuer-certificate-file issuer.crt --identity-issuer-key-file issuer.key | kubectl apply -f -
```

Integrate Openfaas with Linkerd
```
kubectl -n openfaas get deploy gateway -o yaml | linkerd inject --skip-outbound-ports=4222 - | kubectl apply -f -
```

Integrate Nginx Ingress controller with Linkerd
```
kubectl get deploy/nginx-ingress-controller -n ingress-basic -o yaml | linkerd inject - | kubectl apply -f - 
```

Linkerd metrics integration with Prometheus
```
kubectl create secret generic additional-scrape-configs --from-file=yml/linkerd-prometheus-additional.yaml -n monitoring
kubectl edit prometheus  prometheus-prometheus-oper-prometheus  -n monitoring

Add the additionalScrapeConfigs as below
  ....
  ....
  serviceMonitorSelector:
    matchLabels:
      team: frontend
  additionalScrapeConfigs:
    name: additional-scrape-configs
    key: linkerd-prometheus-additional.yaml
  ....
  ....
```

Linkerd integration with Jaeger
```
kubectl  apply -f yml/linkerd-opencesus-collector.yaml -n tracing

kubectl annotate namespace openfaas-fn config.linkerd.io/trace-collector=oc-collector.tracing:55678
kubectl annotate namespace openfaas config.linkerd.io/trace-collector=oc-collector.tracing:55678
kubectl annotate namespace ingress-basic config.linkerd.io/trace-collector=oc-collector.tracing:55678
```

```
kubectl port-forward svc/linkerd-web 8080:8084 -n linkerd
Browse to http://localhost:8080
```

##Tekton
Install Tekton pipelines
```
kubectl apply -f https://storage.googleapis.com/tekton-releases/latest/release.yaml

kubectl apply -f yml/tekton-default-configmap.yaml  -n  tekton-pipelines
kubectl apply -f yml/tekton-pvc-configmap.yaml -n  tekton-pipelines
kubectl apply -f yml/tekton-feature-flags-configmap.yaml -n  tekton-pipelines
```
Install Tekton Triggers
```
kubectl apply --filename https://storage.googleapis.com/tekton-releases/triggers/latest/release.yaml
```
Install Tekton Dashboard
```
kubectl apply --filename https://github.com/tektoncd/dashboard/releases/download/v0.5.2/tekton-dashboard-release.yaml
```
```
kubectl port-forward svc/tekton-dashboard 8080:9097  -n tekton-pipelines
Browse to http://localhost:8080
```

##App Installation

Create the Project and User in Harbor
- Login to Harbor
- Add a new Project with name conexp
- Add a new User under Administration with username as conexp and password as FTA@CNCF0n@zure3
- Associate the user with the conexp project under Memebers tab with a Developer role

Build and push the containers
```
docker login $registryHost
conexp
FTA@CNCF0n@zure3

cd src/Contoso.Expenses.API
docker build -t $registryHost/conexp/api:latest .
# Go to Harbor registry and create **conexp** project
docker push $registryHost/conexp/api:latest

cd ..
docker build -t $registryHost/conexp/web:latest -f Contoso.Expenses.Web/Dockerfile .
docker push $registryHost/conexp/web:latest

docker build -t $registryHost/conexp/emaildispatcher:latest -f Contoso.Expenses.OpenFaaS/Dockerfile .
docker push $registryHost/conexp/emaildispatcher:latest

cd ..

```

```
kubectl create ns conexp-mvp
kubectl annotate namespace conexp-mvp linkerd.io/inject=enabled
kubectl annotate namespace conexp-mvp config.linkerd.io/skip-outbound-ports="4222"
kubectl annotate namespace conexp-mvp config.linkerd.io/trace-collector=oc-collector.tracing:55678
```

Create the registry credentials in the deployment namespaces
```
kubectl create secret docker-registry regcred --docker-server="https://$registryHost" --docker-username=conexp  --docker-password=FTA@CNCF0n@zure3  --docker-email=user@mycompany.com -n conexp-mvp
kubectl create secret docker-registry regcred --docker-server="https://$registryHost" --docker-username=conexp  --docker-password=FTA@CNCF0n@zure3  --docker-email=user@mycompany.com -n openfaas-fn
```

##Tekton - App Deployment

```
kubectl create ns conexp-mvp-devops

kubectl apply -f yml/app-webhook-role.yaml -n conexp-mvp-devops
kubectl apply -f yml/app-admin-role.yaml -n conexp-mvp-devops

kubectl apply -f yml/app-create-ingress.yaml -n conexp-mvp-devops
kubectl apply -f yml/app-create-webhook.yaml -n conexp-mvp-devops
```

Update Secret (basic-user-pass) for registry credentails, TriggerBinding for registry name,namespaces in triggers.yaml
Create a SendGrid Account and set an API key for use
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

Generate PAT token(Settings->Developer settings->Personal access tokens) for the repo -> public_repo, admin:repo_hook, set the pat token below
```
patToken=<<set the pat tokne>>

sed -i "s/{patToken}/$patToken/g" yml/app-github-secret.yaml

kubectl apply -f yml/app-github-secret.yaml -n conexp-mvp-devops
```

set org/user/repo of the source code repo variables below
```
cicdWebhookHost=$topLevelDomain

gitHubOrg=<<set the name of the github org>>
gitHubUser=<<set the name of the github user>>
gitHubRepo=<<set the name of the github repo>>

sed -i "s/{cicdWebhook}/$cicdWebhookHost/g" yml/app-ingress-run.yaml

kubectl apply -f yml/app-ingress-run.yaml  -n conexp-mvp-devops

sed -i "s/{cicdWebhook}/$cicdWebhookHost/g" yml/app-webhook-run.yaml
sed -i "s/{mygithub-org-replace}/$gitHubOrg/g" yml/app-webhook-run.yaml
sed -i "s/{mygithub-user-replace}/$gitHubUser/g" yml/app-webhook-run.yaml
sed -i "s/{mygithub-repo-replace}/$gitHubRepo/g" yml/app-webhook-run.yaml

kubectl apply -f yml/app-webhook-run.yaml -n conexp-mvp-devops
```
