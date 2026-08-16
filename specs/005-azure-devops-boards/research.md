# Phase 0 Research: Intégration Azure DevOps Boards

## 1. Client Azure DevOps REST API

**Decision**: Appeler directement l'API REST Azure DevOps (`https://dev.azure.com/{organisation}/...`,
`api-version=7.1`) via un `HttpClient` typé (`AzureDevOpsClient`), authentifié en Basic Auth
(utilisateur vide, PAT en mot de passe, standard Azure DevOps), plutôt que d'installer un SDK
client officiel (`Microsoft.TeamFoundationServer.Client`).

**Rationale**: Le périmètre de cette feature n'appelle que 4 opérations (valider un PAT/projet,
lister les Area Paths, lister les Iterations, interroger/créer des work items). Le SDK officiel
apporte un grand nombre de dépendances transitives pour un besoin très ciblé — à l'inverse du
Principe VI de la constitution (pas de sur-ingénierie). Un `HttpClient` typé reste cohérent avec le
pattern déjà utilisé pour le Bot Framework (`builder.Services.AddHttpClient()`,
specs/002-poll-utilite-reunion).

**Alternatives considered**:
- **`Microsoft.TeamFoundationServer.Client`** : SDK complet et officiel, mais taille et surface
  disproportionnées pour 4 appels REST simples.
- **Azure.ResourceManager / Azure SDK unifié** : ne couvre pas Azure DevOps Boards (service
  distinct de la gestion de ressources Azure).

## 2. Chiffrement du PAT at-rest

**Decision**: Utiliser l'ASP.NET Core Data Protection API (`IDataProtector`, déjà intégrée au
framework, aucune nouvelle dépendance majeure) pour chiffrer le PAT avant stockage et le déchiffrer
à l'usage, avec les clés de protection persistées dans PostgreSQL via
`Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` (une seule table `DataProtectionKeys`
supplémentaire).

**Rationale**: Data Protection est le mécanisme standard .NET pour ce besoin exact (FR-002,
Constitution — contraintes techniques additionnelles). Par défaut, l'anneau de clés est stocké sur
le disque local du conteneur, ce qui ne survivrait pas à un redémarrage de pod ni ne serait
partagé entre réplicas (le déploiement est sur k3s, Constitution Principe V) : le persister dans
PostgreSQL (déjà la base du projet) est l'option officiellement recommandée par Microsoft pour ce
scénario, sans infrastructure supplémentaire (pas de Redis, pas de stockage partagé).

**Alternatives considered**:
- **Chiffrement manuel (AES avec clé en variable d'environnement)** : réinvente un mécanisme déjà
  fourni et audité par le framework ; rejeté (sur-ingénierie évitable).
- **Anneau de clés sur disque local (défaut)** : ne survit pas aux redémarrages/réplicas multiples
  sur k3s — rejeté pour ce déploiement.

## 3. Résolution du choix guidé d'Area Path (US2, FR-005)

**Decision**: Le champ Area Path à la création d'un board propose, quand elles existent, les
équipes déjà connues du système **et** déjà configurées avec un accès Azure DevOps (US1) — pas un
parcours en direct de l'arbre complet des Area Paths d'une organisation Azure DevOps.

**Rationale**: Une équipe (`Equipe.AreaPath`) et sa configuration Azure DevOps
(`ConfigurationAzureDevOps`) sont associées 1:1 (voir `data-model.md` et Key Entities de
`spec.md`) — il n'existe donc pas encore, avant la sélection de l'Area Path, de PAT/organisation/
projet à interroger pour proposer un arbre Azure DevOps en direct (problème de l'œuf et de la
poule). Proposer les équipes déjà enregistrées et configurées reste une "sélection guidée par des
données réelles" fidèle à FR-005 : chaque Area Path proposé a déjà été validé contre Azure DevOps
au moment de sa configuration (FR-003). Une fois l'Area Path choisi, l'Iteration est alors
récupérée en direct pour cette équipe (FR-005a).

**Alternatives considered**:
- **Sélecteur Organisation → Projet → Area Path en cascade** : permettrait de découvrir de
  nouvelles équipes sans configuration préalable, mais ajoute un flux à 3 niveaux et pose la
  question de la création de l'`Equipe` associée avant qu'un board existe — hors périmètre pour ce
  MVP (une `Equipe` est aujourd'hui toujours créée via son premier board, specs/001-retro-board-base).
- **Champ Area Path toujours en texte libre, avec auto-complétion asynchrone** : ne résout pas le
  problème de l'œuf et de la poule (quel PAT utiliser pour interroger Azure DevOps avant que
  l'Area Path ne soit connu) sans dupliquer un PAT "de découverte" partagé, hors périmètre.

## 4. Détection de l'Iteration en cours (FR-005a)

**Decision**: Utiliser l'endpoint `GET .../_apis/wit/classificationnodes/iterations?$depth=<n>`
(arbre des Iterations avec `attributes.startDate`/`finishDate`), et calculer côté serveur
l'Iteration "en cours" en comparant la date du jour à ces bornes — plutôt que l'endpoint
`_apis/work/teamsettings/iterations` qui retourne un indicateur `timeFrame` déjà calculé mais
nécessite un contexte d'équipe Azure DevOps (Team) distinct de l'Area Path.

**Rationale**: ScrumMaster ne modélise pas de notion de "Team" Azure DevOps (seulement Area Path et
Iteration, specs/001-retro-board-base) ; dépendre de l'endpoint `teamsettings` obligerait à capter
et stocker un nom d'équipe Azure DevOps supplémentaire dans `ConfigurationAzureDevOps`, sans
bénéfice pour le périmètre de cette feature. Comparer les dates soi-même reste une opération
triviale à partir des données déjà nécessaires pour lister les Iterations.

**Alternatives considered**:
- **Endpoint `teamsettings/iterations`** : rejeté pour la raison ci-dessus (dépendance à un concept
  Team non modélisé).

## 5. Import de work items (US3) et validité de l'Iteration du board

**Decision**: L'import (FR-008) interroge Azure DevOps par une requête WIQL filtrée sur
`[System.IterationPath] = '<Board.Iteration>'`. Cela suppose que `Board.Iteration` contient le
chemin d'Iteration réel Azure DevOps — garanti uniquement lorsque le board a été créé via la
sélection guidée (US2, équipe configurée). Lorsqu'un board a été créé avec un Area Path/Iteration
en texte libre (FR-006/FR-007, équipe non configurée ou Azure DevOps injoignable), l'import ne
retournera simplement aucun work item (comportement déjà couvert par l'Edge Case "itération sans
aucun work item assigné" de `spec.md`), sans erreur additionnelle à traiter.

**Rationale**: Évite d'introduire une distinction supplémentaire ("Iteration validée" vs "Iteration
texte libre") dans le modèle de données — le comportement de repli est déjà correct par
construction (une recherche WIQL sur un chemin de texte libre qui ne correspond à aucune Iteration
réelle ne retournera simplement aucun résultat).

**Alternatives considered**:
- **Bloquer l'import si le board n'a pas été créé via la sélection guidée** : ajouterait un état
  à suivre par board pour un bénéfice marginal (le comportement de repli silencieux est déjà
  correct).

## 6. Transport des actions d'import/export

**Decision**: `ConfigurerAccesAzureDevOps` (US1) et la lecture des Area Paths/Iterations
disponibles (US2) sont exposés en REST (comme `GET /api/themes`, `POST /api/boards` de
specs/001-retro-board-base — actions hors session de board). L'import de work items (US3) et
l'export d'un post-it (US4) sont exposés comme nouvelles méthodes du hub SignalR existant
(`RetroBoardHub`), au même titre que `ChangeTheme`/`CloseBoard`, car ce sont des mutations de
contenu du board devant être diffusées en temps réel à tous les participants connectés
(specs/001-retro-board-base, contracts/realtime-hub.md).

**Rationale**: Cohérent avec la séparation déjà établie dans specs/001-retro-board-base : REST
pour le cycle de vie hors session, SignalR pour toute mutation de contenu à l'intérieur d'une
session de board active.

**Alternatives considered**: Aucune — la convention est déjà posée par specs/001-retro-board-base.
