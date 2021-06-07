#!/usr/bin/env bash
email=$1

sleep 30

cat <<EOF | kubectl create -f -
apiVersion: cert-manager.io/v1alpha2
kind: ClusterIssuer
metadata:
  name: letsencrypt
  namespace: ingress-basic
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: $email
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