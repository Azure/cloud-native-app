
### Install Flux

```bash
curl -s https://fluxcd.io/install.sh | sudo bash
```

### Prepare the repo

- Fork the repo
- Clone the fork to workstation

#### Generate Linkerd v2 certificates

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

cd gitops/app/core

kubectl create secret docker-registry regcred \
--docker-server="https://$registryHost" --docker-username=conexp  --docker-password=FTA@CNCF0n@zure3  --docker-email=user@mycompany.com -n conexp-mvp -oyaml --dry-run=client \
> regcred-conexp.yaml

kubectl create secret docker-registry regcred \
--docker-server="https://$registryHost" --docker-username=conexp  --docker-password=FTA@CNCF0n@zure3  --docker-email=user@mycompany.com -n openfaas-fn -oyaml --dry-run=client \
> regcred-openfaas.yaml

cd ../../..

```

Commit the Repo

### Bootstrap

- Create a PAT token in Github
- Run the bootstrap

```bash
export GITHUB_TOKEN=<PAT TOken>

flux bootstrap github \
  --owner=<OrgName> \
  --repository=cloud-native-app \
  --path=gitops/clusters/production \
  --personal
```

### Reconciliation

Update the DNS labels on the public IPs as they are provisioned
