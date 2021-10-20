#!/bin/bash

echo "## Installing Flux"
curl -s https://fluxcd.io/install.sh | sudo bash

echo "## Installing Kube Seal"
wget https://github.com/bitnami-labs/sealed-secrets/releases/download/v0.16.0/kubeseal-linux-amd64 -O kubeseal
sudo install -m 755 kubeseal /usr/local/bin/kubeseal && rm kubeseal

echo "##Flux Bootstap with kubeseal"
flux bootstrap github \
  --owner="$owner" \
  --repository=cloud-native-app \
  --path=gitops/clusters/bootstrap \
  --personal

echo "## Download kube seal publick key"
kubeseal --fetch-cert \
--controller-name=sealed-secrets-controller \
--controller-namespace=flux-system \
> pub-sealed-secrets.pem
  
echo "## Install Step"
sudo wget https://github.com/smallstep/cli/releases/download/v0.15.2/step-cli_0.15.2_amd64.deb
sudo dpkg -i step-cli_0.15.2_amd64.deb

cd cloud-native-app/gitops/infrastructure/linkerd

echo "## Generate Certificates for linkerd"
step certificate create identity.linkerd.cluster.local ca.crt ca.key \
	--profile root-ca --no-password --insecure \
	--san identity.linkerd.cluster.local

step certificate create identity.linkerd.cluster.local issuer.crt issuer.key \
	--ca ca.crt --ca-key ca.key --profile intermediate-ca --not-after 8760h --no-password --insecure \
	--san identity.linkerd.cluster.local

echo "## Generate Secrets from Certs"
kubectl -n linkerd create secret generic certs \
	--from-file=ca.crt --from-file=issuer.crt \
	--from-file=issuer.key -oyaml --dry-run=client \
	> certs.yaml

echo "# Seal Certs Secrets"
kubeseal --format=yaml --cert=../../../../pub-sealed-secrets.pem \
< certs.yaml > certs-sealed.yaml
rm certs.yaml

cd ../../..

registryUrl=https://$registryHost
appHostDnsLabel=`echo $appHostName | cut -d '.' -f 1`
registryHostDnsLabel=`echo $registryHost | cut -d '.' -f 1`

echo "## Replace tokens in yamls"
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

echo "## Generate registry secrets for conexp namespace"
kubectl create secret docker-registry regcred \
	--docker-server="https://$registryHost" --docker-username=conexp  --docker-password=FTA@CNCF0n@zure3  --docker-email=user@mycompany.com -n conexp-mvp -oyaml --dry-run=client \
	> regcred-conexp.yaml

echo "## Seal the secret"
kubeseal --format=yaml --cert=../../../../pub-sealed-secrets.pem \
< regcred-conexp.yaml > regcred-conexp-sealed.yaml
rm regcred-conexp.yaml

echo "## Generate registry secrets for openfaas namespace"
kubectl create secret docker-registry regcred \
	--docker-server="https://$registryHost" --docker-username=conexp  --docker-password=FTA@CNCF0n@zure3  --docker-email=user@mycompany.com -n openfaas-fn -oyaml --dry-run=client \
	> regcred-openfaas.yaml

echo "## Seal the secret"
kubeseal --format=yaml --cert=../../../../pub-sealed-secrets.pem \
< regcred-openfaas.yaml > regcred-openfaas-sealed.yaml
rm regcred-openfaas.yaml

cd ../../..

echo "## Commit the changes to the git repo"
git remote set-url origin "https://$owner:$GITHUB_TOKEN@github.com/$owner/cloud-native-app.git"
git config user.email "$cluster_issuer_email"
git config user.name "Auto"
git pull
git add *
git commit -m  "Auto commit prep files"
git push -u origin main

echo "## Add Kustomization to reconcile the app"
flux create kustomization cloud-native-app \
  --depends-on=flux-system \
  --source=seenu433/cloud-native-app \
  --path="gitops/clusters/production" \
  --prune=true \
  --interval=5m

echo "## Create the webhook for continous integration"
curl -H "Authorization: token $GITHUB_TOKEN" \
	  -X POST  \
	    -H "Accept: application/vnd.github.v3+json" \
	      https://api.github.com/repos/$owner/cloud-native-app/hooks \
	        -d "{\"config\":{\"url\":\"https://$appHostName/cd\",\"content_type\":\"json\"}}"

