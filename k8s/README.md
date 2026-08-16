# Déploiement ScrumMaster

Manifests Kustomize pour le cluster k3s partagé avec SkillForge (Constitution Principe V —
namespace, base de données et manifests strictement séparés).

## Construire et pousser les images

```bash
docker build -t ghcr.io/matteobadet/scrummaster-api:latest backend/src/ScrumMaster.Api
docker push ghcr.io/matteobadet/scrummaster-api:latest

docker build -t ghcr.io/matteobadet/scrummaster-frontend:latest frontend
docker push ghcr.io/matteobadet/scrummaster-frontend:latest
```

## Base de données

Créer une base `scrummaster` (et un rôle dédié) sur l'instance PostgreSQL déjà présente sur le
cluster — aucun Deployment Postgres n'est inclus ici. Les migrations EF Core s'appliquent
automatiquement au démarrage du pod API (voir `Program.cs`).

## Déployer

```bash
cp k8s/overlays/production/connection.env.example k8s/overlays/production/connection.env
# éditer connection.env avec la vraie chaîne de connexion (fichier non commité, voir .gitignore)

kubectl apply -k k8s/overlays/production
```

Éditer `k8s/overlays/production/ingress.yaml` pour remplacer `scrummaster.example.com` par le
domaine réel avant application.
