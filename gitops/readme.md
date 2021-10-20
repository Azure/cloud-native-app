# GitOps Deployment Options

## Prepare

- Create a Kubernetes cluster with a minimum of 3 nodes and ~8+GB per node (e.g., Standard_DS3_v2). Ensure the cluster is reachable by running any kubectl command.
- Fork the repo
- Create a [PAT token](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token) for the repo
- Clone the fork to workstation

## Option 1

### Script Install

```bash
# PAT token to access Github
export GITHUB_TOKEN="<<PAT TOken>>"
# Github runner
export owner="<<Github user>>"
# FQDN to assign to the Harbor Ingress. eg. {uniquename}.{region}.cloudapp.azure.com if assigning through the Configuration blade of a Azure PublicIP
export registryHost="<<FQDN>>"
# FQDN to assign to the app eg. {uniquename}.{region}.cloudapp.azure.com if assigning through the Configuration blade of a Azure PublicIP
export appHostName="<<FQDN>>"
# Email to use for LetsEncrypt
export cluster_issuer_email="<<EMAIL>>"
# sendGrid API Key for the app to send emails
export sendGridApiKey="<<set the api key>>"


. ./cloud-native-app/gitops/setup.sh

```
The cluster components will take around 12 minutes to deploy. You can check the status (all should read True) with the below command

```bash
kubectl get Kustomizations -A
```

### Urls for the components

```bash
# Tekton
kubectl port-forward svc/tekton-dashboard 8080:9097  -n tekton-pipelines
Browse to http://localhost:8080

# Linkerd
kubectl port-forward svc/web 8084:8084  -n linkerd-viz
Browse to http://localhost:8084

#Jaeger
kubectl port-forward svc/jaeger-query 8060:80 -n tracing
Browse to http://localhost:8060

# Grafana
kubectl port-forward deploy/prometheus-grafana 8070:3000 -n monitoring
Browse to http://localhost:8070 and use the username/password as admin/FTA@CNCF0n@zure3

# Prometheus
kubectl port-forward svc/prometheus-kube-prometheus-prometheus 9090:9090 -n monitoring 
Browse to http://localhost:9090

# Openfaas
kubectl port-forward deploy/gateway 8080:8080 -n openfaas
Browse to http://localhost:8080 and use the username/password as admin/FTA@CNCF0n@zure3
```

Invoke the CICD pipeline by making a small edit to the read.me file in Github. Observe the deployment in Tekton Dashboard. The app deployment should take around 5 minutes.

Navigate to the appHost in the browser to test the app.

## Option 2

Alternatively, use the instructions below

### Install Flux

```bash
curl -s https://fluxcd.io/install.sh | sudo bash
```

#### Generate Linkerd v2 certificates

### Install Kube-Seal

```bash
wget https://github.com/bitnami-labs/sealed-secrets/releases/download/v0.16.0/kubeseal-linux-amd64 -O kubeseal
sudo install -m 755 kubeseal /usr/local/bin/kubeseal && rm kubeseal
```

### Bootstrap

- Create a PAT token in Github
- Run the bootstrap

```bash
# PAT token to access Github
export GITHUB_TOKEN=<PAT TOken>
# Github runner
export owner="<<Github user>>"

flux bootstrap github \
  --owner="$owner" \
  --repository=cloud-native-app \
  --path=gitops/clusters/bootstrap \
  --personal
  
```

### Prepare Repo

```bash

wget https://github.com/smallstep/cli/releases/download/v0.15.2/step-cli_0.15.2_amd64.deb
sudo dpkg -i step-cli_0.15.2_amd64.deb

cd cloud-native-app/gitops/infrastructure/linkerd

step certificate create identity.linkerd.cluster.local ca.crt ca.key \
--profile root-ca --no-password --insecure \
--san identity.linkerd.cluster.local

step certificate create identity.linkerd.cluster.local issuer.crt issuer.key \
--ca ca.crt --ca-key ca.key --profile intermediate-ca --not-after 8760h --no-password --insecure \
--san identity.linkerd.cluster.local

kubectl -n linkerd create secret generic certs \
--from-file=ca.crt --from-file=issuer.crt \
--from-file=issuer.key -oyaml --dry-run=client \
> certs.yaml

kubeseal --fetch-cert \
--controller-name=sealed-secrets-controller \
--controller-namespace=flux-system \
> ../../../../pub-sealed-secrets.pem

kubeseal --format=yaml --cert=../../../../pub-sealed-secrets.pem \
< certs.yaml > certs-sealed.yaml
rm certs.yaml

cd ../../..

```

Set the variables required for the deployment

```bash

# Email to use for LetsEncrypt
cluster_issuer_email="<<EMAIL>>"
# FQDN to assign to the Harbor Ingress. Eg. {uniquename}.{region}.cloudapp.azure.com if assigning through the Configuration blade of a Azure PublicIP
registryHost="<<FQDN>>"
# sendGrid API Key for the app to send emails
sendGridApiKey="<<set the api key>>"
# FQDN to assign to the app Eg. {uniquename}.{region}.cloudapp.azure.com if assigning through the Configuration blade of a Azure PublicIP
appHostName="<<FQDN>>"

registryUrl=https://$registryHost
appHostDnsLabel=`echo $appHostName | cut -d '.' -f 1`
registryHostDnsLabel=`echo $registryHost | cut -d '.' -f 1`
exp=$(date -d '+8760 hour' +"%Y-%m-%dT%H:%M:%SZ")
sed -i "s/{cert_expiry}/$exp/g" gitops/clusters/production/infrastructure-linkerd.yaml
sed -i "s/{registryHost}/$registryHost/g" gitops/clusters/production/infrastructure-harbor.yaml
sed -i "s%{registryUrl}%$registryUrl%g" gitops/clusters/production/infrastructure-harbor.yaml
sed -i "s%{registryUrl}%$registryUrl%g" gitops/clusters/production/infrastructure-seed.yaml
sed -i "s/{cluster_issuer_email}/$cluster_issuer_email/g" gitops/clusters/production/infrastructure-certmanager.yaml

sed -i "s/{cicdWebhookHost}/$appHostName/g" gitops/clusters/production/app-devops.yaml
sed -i "s/{registryHost}/$registryHost/g" gitops/clusters/production/app-devops.yaml
sed -i "s/{appHostName}/$appHostName/g" gitops/clusters/production/app-devops.yaml
sed -i "s/{sendGridApiKey}/$sendGridApiKey/g" gitops/clusters/production/app-devops.yaml

sed -i "s/{registryHostDnsLabel}/$registryHostDnsLabel/g" gitops/clusters/production/infrastructure-harbor-nginx.yaml
sed -i "s/{appHostDnsLabel}/$appHostDnsLabel/g" gitops/clusters/production/infrastructure-nginx.yaml

cd gitops/app/core

kubectl create secret docker-registry regcred \
--docker-server="https://$registryHost" --docker-username=conexp  --docker-password=FTA@CNCF0n@zure3  --docker-email=user@mycompany.com -n conexp-mvp -oyaml --dry-run=client \
> regcred-conexp.yaml

kubeseal --format=yaml --cert=../../../../pub-sealed-secrets.pem \
< regcred-conexp.yaml > regcred-conexp-sealed.yaml
rm regcred-conexp.yaml

kubectl create secret docker-registry regcred \
--docker-server="https://$registryHost" --docker-username=conexp  --docker-password=FTA@CNCF0n@zure3  --docker-email=user@mycompany.com -n openfaas-fn -oyaml --dry-run=client \
> regcred-openfaas.yaml

kubeseal --format=yaml --cert=../../../../pub-sealed-secrets.pem \
< regcred-openfaas.yaml > regcred-openfaas-sealed.yaml
rm regcred-openfaas.yaml

cd ../../..

```

Commit the Repo

### Deploy the app

```bash

  flux create kustomization cloud-native-app \
    --depends-on=flux-system \
    --source=seenu433/cloud-native-app \
    --path="gitops/clusters/production" \
    --prune=true \
    --interval=5m
    
```

### Reconciliation

Create a webhook for the Github

```bash
curl -H "Authorization: token $GITHUB_TOKEN" \
  -X POST  \
  -H "Accept: application/vnd.github.v3+json" \
  https://api.github.com/repos/$owner/cloud-native-app/hooks \
  -d "{\"config\":{\"url\":\"https://$appHostName/cd\",\"content_type\":\"json\"}}"
```
