# Research: Historique des boards par équipe

## 1. Emplacement de l'endpoint de liste

**Decision**: Exposer `GET /api/equipes/{areaPath}/boards` directement dans `BoardsController`
(route absolue via `[HttpGet("/api/equipes/{areaPath}/boards")]`), plutôt que de créer un nouveau
contrôleur `EquipesController` ou d'ajouter cette route à `AzureDevOpsController`.

**Rationale**: `AzureDevOpsController` est sémantiquement scopé à l'intégration Azure DevOps
(specs/005-azure-devops-boards) — cette liste de boards n'a aucune dépendance à cette intégration
(elle fonctionne pour toute équipe, configurée ou non). Créer un contrôleur dédié pour une seule
route serait de la sur-ingénierie (Constitution Principe VI) ; ASP.NET Core supporte nativement une
route absolue par action, donc `BoardsController` (déjà propriétaire du modèle `Board`) peut exposer
cette route sans forcer son préfixe `api/boards`.

**Alternatives considered**: Nouveau `EquipesController` — rejeté pour une seule route MVP, peut
être introduit plus tard si d'autres endpoints scopés "équipe" (hors Azure DevOps) apparaissent.

## 2. Pas de mise à jour temps réel de la liste

**Decision**: La liste est récupérée par un simple `GET` REST à l'ouverture de la page, sans
connexion SignalR — cohérent avec l'Assumption de spec.md et le pattern déjà utilisé par
`GET /api/equipes/avec-azure-devops` (specs/005-azure-devops-boards) pour une liste similaire de
portée "équipe".

**Rationale**: L'historique est une page de consultation ponctuelle (on y arrive, on repart), pas
une vue collaborative en direct comme la page d'un board — aucune des pages de portée "équipe"
existantes (config Azure DevOps, sélection d'Iteration) n'utilise de canal temps réel.

## 3. Aucune nouvelle entité ni migration

**Decision**: La liste est une projection directe de `Board` (déjà doté de `AreaPath`, `Iteration`,
`Statut`, `DateCreation` — voir `backend/src/ScrumMaster.Api/Models/Board.cs`), filtrée et triée en
mémoire par EF Core. Aucune migration nécessaire.

**Rationale**: Toutes les données requises par FR-002 existent déjà sur l'entité `Board` depuis
specs/001-retro-board-base ; cette feature n'ajoute qu'une requête de lecture.
