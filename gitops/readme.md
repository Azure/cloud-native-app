# GitOps Deployment Options

## Prepare

- Create a Kubernetes cluster with a minimum of 3 nodes and ~8+GB per node (e.g., Standard_DS3_v2). Do not deploy any network policy plugin. Ensure the cluster is reachable by running any kubectl command.
- Fork the repo
- Create a [PAT token](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token) for the repo (check the repo box as scope)
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

# Harbor Url
echo $registryHost

# App Url
echo $appHostName

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
sudo wget https://github.com/bitnami-labs/sealed-secrets/releases/download/v0.22.0/kubeseal-0.22.0-linux-amd64.tar.gz -O kubeseal.tar.gz
tar -xvzf kubeseal.tar.gz kubeseal && rm kubeseal.tar.gz
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

wget https://github.com/smallstep/cli/releases/download/v0.23.4/step-cli_0.23.4_amd64.deb
sudo dpkg -i step-cli_0.23.4_amd64.deb

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

kubectl rollout status deployment sealed-secrets-controller -n flux-system

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

kubectl create secret generic gitops-variables --from-literal=registryHost=$registryHost \
	--from-literal=registryUrl=$registryUrl \
	--from-literal=externalUrl=$registryUrl \
	--from-literal=cluster_issuer_email=$cluster_issuer_email  \
	--from-literal=cicdWebhookHost=$appHostName \
	--from-literal=appHostName=$appHostName \
	--from-literal=sendGridApiKey=$sendGridApiKey \
	--from-literal=registryHostDnsLabel=$registryHostDnsLabel \
	--from-literal=appHostDnsLabel=$appHostDnsLabel \
	--from-literal=cert_expiry=$exp \
	-n flux-system -oyaml --dry-run=client \
	> gitops-variables.yaml
  
kubeseal --format=yaml --cert=../pub-sealed-secrets.pem \
< gitops-variables.yaml > gitops-variables-sealed.yaml  

rm gitops-variables.yaml

kubectl apply -f gitops-variables-sealed.yaml

rm gitops-variables-sealed.yaml

cd gitops/app/core

kubectl create secret docker-registry regcred \
--docker-server="https://$registryHost" --docker-username=conexp  --docker-password=FTA@CNCF0n@zure3  --docker-email=user@mycompany.com -n conexp-mvp -oyaml --dry-run=client \
> regcred-conexp.yaml

kubeseal --format=yaml --cert=../../../../pub-sealed-secrets.pem \
< regcred-conexp.yaml > regcred-conexp-sealed.yaml
rm regcred-conexp.yaml

kubectl create secret docker-registry regcred \
--docker-server="https://$registryHost" --docker-username=conexp  --docker-password=FTA@CNCF0n@zure3  --docker-email=user@mycompany.com -n conexp-mvp-fn -oyaml --dry-run=client \
> regcred-fn.yaml

kubeseal --format=yaml --cert=../../../../pub-sealed-secrets.pem \
< regcred-fn.yaml > regcred-fn-sealed.yaml
rm regcred-fn.yaml

cd ../../..

cd gitops/app/devops

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
kubectl create secret generic regcred --from-file=config.json=config.json  -oyaml --dry-run=client  > regcred-devops.yaml
rm config.json

kubeseal --format=yaml --cert=../../../../pub-sealed-secrets.pem \
< regcred-devops.yaml > regcred-devops-sealed.yaml
rm regcred-devops.yaml

cd ../../..

```

Commit the Repo

### Deploy the app

```bash

  flux create kustomization cloud-native-app \
    --depends-on=flux-system \
    --source=flux-system \
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

Test Stub #2
