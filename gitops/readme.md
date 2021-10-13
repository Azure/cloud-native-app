
### Install Flux

```bash
curl -s https://fluxcd.io/install.sh | sudo bash
```

### Update Yamls
Update the substitution variables in yamls bleow
infrastructure-certmanager.yaml
infrastructure-harbor.yaml

### Bootstrap
- Fork the Repo
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
