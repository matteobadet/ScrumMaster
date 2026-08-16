<!--
Sync Impact Report
Version change: none → 1.0.0 (initial ratification)
Modified principles: n/a (initial adoption)
Added sections:
  - Core Principles: I. Développement piloté par les spécifications, II. Stack technique standardisée,
    III. MVP avant tout, IV. Multi-tenant par conception, V. Isolation du déploiement partagé,
    VI. Évolutivité sans sur-ingénierie
  - Contraintes techniques additionnelles
  - Workflow de développement
  - Governance
Removed sections: n/a
Follow-up TODOs: none
-->

# ScrumMaster Constitution

## Core Principles

### I. Développement piloté par les spécifications (NON-NÉGOCIABLE)
Chaque fonctionnalité DOIT suivre le workflow speckit dans l'ordre : `/speckit-specify` →
`/speckit-clarify` → `/speckit-plan` → `/speckit-tasks` → `/speckit-implement`. Aucun code
d'implémentation (application, infrastructure, migration de base de données) ne DOIT être écrit
avant qu'une spécification et un plan technique correspondants existent et soient validés par
l'utilisateur. Les ambiguïtés fonctionnelles identifiées pendant la spécification DOIVENT être
levées via `/speckit-clarify` plutôt que par une supposition de l'agent.
**Rationale**: Le projet est développé par un agent IA en collaboration avec un unique décideur
produit ; sans ce garde-fou, des décisions fonctionnelles ou techniques non désirées peuvent être
figées dans le code avant d'avoir été discutées.

### II. Stack technique standardisée
Le backend et le bot Teams DOIVENT être implémentés en ASP.NET Core (C#) avec le Bot Framework
SDK. Le frontend interactif (Tab Teams) DOIT être implémenté en React avec le Teams JS SDK. La
persistance DOIT utiliser PostgreSQL. Tout écart par rapport à cette stack (nouveau langage,
framework frontend alternatif, autre SGBD) DOIT être justifié explicitement dans le plan technique
de la fonctionnalité concernée et validé par l'utilisateur avant implémentation.
**Rationale**: Cohérence avec l'infrastructure existante (projet SkillForge) et réduction de la
charge opérationnelle liée à la diversité technologique sur un même cluster.

### III. MVP avant tout
L'ordre de construction des fonctionnalités DOIT respecter la priorité suivante, sans
parallélisation ni anticipation : (1) board de rétrospective interactif de base — colonnes,
post-its, vote, thème modifiable, pour une seule équipe ; (2) poll d'utilité de réunion et
invitations Teams ; (3) intégration Azure DevOps Boards en lecture/écriture ; (4) système
d'extensions/plugins. Une fonctionnalité d'une phase ultérieure ne DOIT pas être spécifiée ni
implémentée avant que les fonctionnalités des phases précédentes soient fonctionnelles.
**Rationale**: Valider la valeur du cœur produit (l'animation de rétrospective) avant d'investir
dans les intégrations Teams et Azure DevOps, qui sont coûteuses à mettre en place (enregistrement
Azure AD, permissions) et sans valeur si le board lui-même ne fonctionne pas.

### IV. Multi-tenant par conception
Même lorsque le MVP est développé et testé pour une seule équipe, les modèles de données et les
API DOIVENT être conçus de sorte qu'une identité d'équipe (tenant) soit un attribut explicite des
entités persistées (board, colonnes, post-its, configuration), et non ajoutée rétroactivement.
Toute requête de lecture/écriture DOIT être scopée à un tenant. Le remplissage réel du
multi-tenant (isolation stricte, provisioning multi-équipes) peut être différé à une fonctionnalité
ultérieure, mais aucune décision de modélisation ne DOIT rendre cette isolation impossible ou
coûteuse à ajouter plus tard.
**Rationale**: Le produit est explicitement destiné à plusieurs équipes indépendantes ; retrofitter
le multi-tenant sur un modèle de données mono-équipe est une source connue de migrations
douloureuses.

### V. Isolation du déploiement partagé
ScrumMaster est déployé sur le même cluster k3s que SkillForge mais DOIT avoir sa propre base de
données PostgreSQL dédiée (nouvelle base sur l'instance Postgres existante, jamais de schéma
partagé avec SkillForge) et ses propres manifests Kustomize sous `k8s/`. Aucun changement DE
déploiement de ScrumMaster ne DOIT modifier les ressources, namespaces ou configurations Traefik/
cert-manager appartenant à SkillForge.
**Rationale**: Éviter qu'une régression ou un incident sur ScrumMaster n'affecte la disponibilité
de SkillForge, qui est déjà en production.

### VI. Évolutivité sans sur-ingénierie
Le système d'extensions/plugins (étapes personnalisées du board, mini-jeux, polls custom) est une
intention d'architecture future et DOIT rester hors du périmètre d'implémentation tant qu'il n'a
pas sa propre spécification. Les choix de modélisation du board (structure des étapes, séquencement)
DOIVENT éviter les décisions qui fermeraient explicitement la porte à une extensibilité future
(ex: coder en dur un nombre fixe d'étapes non paramétrable), sans pour autant concevoir l'API de
plugin, le cycle de vie d'une étape custom ou l'isolation d'exécution avant que ce travail ne soit
spécifié.
**Rationale**: Anticiper une architecture de plugin non spécifiée risque de produire une
abstraction prématurée et incorrecte ; il suffit de ne pas se fermer de portes.

## Contraintes techniques additionnelles

- **Secrets Azure DevOps** : les Personal Access Tokens saisis par les équipes DOIVENT être
  stockés chiffrés at-rest et ne DOIVENT jamais apparaître en clair dans les logs applicatifs, les
  messages d'erreur, ou les réponses API.
- **Intégration Teams** : le bot (notifications, polls, invitations) et le tab (board interactif)
  DOIVENT utiliser respectivement le Bot Framework SDK et le Teams JS SDK ; toute fonctionnalité
  d'interaction en temps réel côté tab DOIT être définie dans le plan technique de la fonctionnalité
  concernée, y compris son mécanisme de synchronisation (le choix technique n'est pas encore arrêté
  au niveau de cette constitution).
- **Ingress et TLS** : toute exposition HTTP de ScrumMaster DOIT passer par Traefik avec un
  certificat géré par cert-manager, conformément au reste du cluster.

## Workflow de développement

Pour chaque fonctionnalité, dans cet ordre :
1. `/speckit-specify` — spécification fonctionnelle, sans décision technique.
2. `/speckit-clarify` — résolution des ambiguïtés avant de planifier.
3. `/speckit-plan` — traduction en plan technique conforme à la Stack technique standardisée
   (Principe II) et aux Contraintes techniques additionnelles.
4. `/speckit-tasks` — découpage en tâches actionnables.
5. `/speckit-implement` — exécution, uniquement une fois les étapes précédentes validées par
   l'utilisateur.

Aucune étape ne DOIT être sautée. Si une étape révèle qu'une décision antérieure (y compris de
cette constitution) doit changer, l'amendement DOIT être proposé explicitement plutôt que contourné
silencieusement.

## Governance

Cette constitution prévaut sur toute autre pratique ou préférence implicite pour le projet
ScrumMaster. Toute proposition de plan technique ou de spécification qui contredit un principe
DOIT soit être révisée pour s'y conformer, soit s'accompagner d'une proposition d'amendement
explicite à cette constitution, validée par l'utilisateur avant de continuer.

**Procédure d'amendement** : toute modification de ce document DOIT être proposée à l'utilisateur
avec un résumé des changements et leur justification avant d'être écrite. Un amendement approuvé
DÉCLENCHE une incrémentation de version selon la politique de versionnage sémantique ci-dessous.

**Politique de versionnage** :
- MAJOR : suppression ou redéfinition incompatible d'un principe existant.
- MINOR : ajout d'un principe ou d'une section, ou expansion matérielle d'une exigence existante.
- PATCH : clarifications, corrections de formulation, changements non sémantiques.

**Revue de conformité** : chaque plan technique produit par `/speckit-plan` DOIT inclure une
vérification explicite de conformité aux principes de cette constitution avant de passer à
`/speckit-tasks`.

**Version**: 1.0.0 | **Ratified**: 2026-08-16 | **Last Amended**: 2026-08-16
