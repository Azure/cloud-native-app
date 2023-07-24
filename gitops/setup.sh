#!/bin/bash

echo "## Installing Flux"
curl -s https://fluxcd.io/install.sh | sudo bash

echo "## Installing Kube Seal"
sudo wget https://github.com/bitnami-labs/sealed-secrets/releases/download/v0.22.0/kubeseal-0.22.0-linux-amd64.tar.gz -O kubeseal.tar.gz
tar -xvzf kubeseal.tar.gz kubeseal && rm kubeseal.tar.gz
sudo install -m 755 kubeseal /usr/local/bin/kubeseal && rm kubeseal

echo "##Flux Bootstap with kubeseal"
flux bootstrap github \
  --owner="$owner" \
  --repository=cloud-native-app \
  --path=gitops/clusters/bootstrap \
  --personal

echo "## Install Step"
sudo wget https://github.com/smallstep/cli/releases/download/v0.23.4/step-cli_0.23.4_amd64.deb
sudo dpkg -i step-cli_0.23.4_amd64.deb

cd gitops/infrastructure/linkerd

echo "## Generate Certificates for linkerd"
sudo step certificate create identity.linkerd.cluster.local ca.crt ca.key \
	--profile root-ca --no-password --insecure \
	--san identity.linkerd.cluster.local

sudo step certificate create identity.linkerd.cluster.local issuer.crt issuer.key \
	--ca ca.crt --ca-key ca.key --profile intermediate-ca --not-after 8760h --no-password --insecure \
	--san identity.linkerd.cluster.local

echo "## Generate Secrets from Certs"
sudo sh -c "kubectl -n linkerd create secret generic certs \
	--from-file=ca.crt --from-file=issuer.crt \
	--from-file=issuer.key -oyaml --dry-run=client \
	> certs.yaml"

echo "## Checking kube seal roolout status"
kubectl rollout status deployment sealed-secrets-controller -n flux-system

echo "## Download kube seal publick key"
sudo sh -c "kubeseal --fetch-cert \
--controller-name=sealed-secrets-controller \
--controller-namespace=flux-system \
> ../../../pub-sealed-secrets.pem"

echo "# Seal Certs Secrets"
sudo sh -c "kubeseal --format=yaml --cert=../../../pub-sealed-secrets.pem \
< certs.yaml > certs-sealed.yaml"
sudo rm certs.yaml ca.crt issuer.crt issuer.key ca.key

cd ../../..

registryUrl=https://$registryHost
appHostDnsLabel=`echo $appHostName | cut -d '.' -f 1`
registryHostDnsLabel=`echo $registryHost | cut -d '.' -f 1`

echo "## Replace tokens in yamls"
exp=$(date -d '+8760 hour' +"%Y-%m-%dT%H:%M:%SZ")

echo "## Generate secret for variable substitution"
sudo sh -c "kubectl create secret generic gitops-variables --from-literal=registryHost=$registryHost \
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
	> gitops-variables.yaml"

echo "## Seal secret for variable substitution"
sudo sh -c "kubeseal --format=yaml --cert=pub-sealed-secrets.pem \
< gitops-variables.yaml > gitops-variables-sealed.yaml"
sudo rm gitops-variables.yaml	

echo "## Apply secret for variable substitution"
kubectl apply -f gitops-variables-sealed.yaml

sudo rm gitops-variables-sealed.yaml

cd gitops/app/core

echo "## Generate registry secrets for conexp namespace"
sudo sh -c "kubectl create secret docker-registry regcred \
	--docker-server="https://$registryHost" --docker-username=conexp  --docker-password=FTA@CNCF0n@zure3  --docker-email=user@mycompany.com -n conexp-mvp -oyaml --dry-run=client \
	> regcred-conexp.yaml"

echo "## Seal the secret"
sudo sh -c "kubeseal --format=yaml --cert=../../../pub-sealed-secrets.pem \
< regcred-conexp.yaml > regcred-conexp-sealed.yaml"
sudo rm regcred-conexp.yaml

echo "## Generate registry secrets for conexp-mvp-fn namespace"
sudo sh -c "kubectl create secret docker-registry regcred \
	--docker-server="https://$registryHost" --docker-username=conexp  --docker-password=FTA@CNCF0n@zure3  --docker-email=user@mycompany.com -n conecp-mvp-fn -oyaml --dry-run=client \
	> regcred-fn.yaml"

echo "## Seal the secret"
sudo sh -c "kubeseal --format=yaml --cert=../../../pub-sealed-secrets.pem \
< regcred-fn.yaml > regcred-fn-sealed.yaml"
sudo rm regcred-fn.yaml

cd ../../..

cd gitops/app/devops

echo "## Create dockerconfig for tekton pipelines"
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

echo "## Generate registry secrets for tekton pipelines"
sudo sh -c "kubectl create secret generic regcred --from-file=config.json=config.json -n conexp-mvp-devops -oyaml --dry-run=client  > regcred-devops.yaml"
rm config.json

echo "## Seal the secret"
sudo sh -c "kubeseal --format=yaml --cert=../../../pub-sealed-secrets.pem \
< regcred-devops.yaml > regcred-devops-sealed.yaml"
rm regcred-devops.yaml

cd ../../..

rm step-cli_0.23.4_amd64.deb pub-sealed-secrets.pem

echo "## Commit the changes to the git repo"
sudo git remote set-url origin "https://$owner:$GITHUB_TOKEN@github.com/$owner/cloud-native-app.git"
sudo git config user.email "$cluster_issuer_email"
sudo git config user.name "Auto"
sudo git pull
sudo git add *
sudo git commit -m  "Auto commit prep files"
sudo git push -u origin main

echo "## Add Kustomization to reconcile the app"
flux create kustomization cloud-native-app \
  --depends-on=flux-system \
  --source=flux-system \
  --path="gitops/clusters/production" \
  --prune=true \
  --interval=5m

echo "## Create the webhook for continous integration"
curl -H "Authorization: token $GITHUB_TOKEN" \
	  -X POST  \
	    -H "Accept: application/vnd.github.v3+json" \
	      https://api.github.com/repos/$owner/cloud-native-app/hooks \
	        -d "{\"config\":{\"url\":\"https://$appHostName/cd\",\"content_type\":\"json\"}}"

