# Research: Point de sprint (stats Azure DevOps)

## 1. Catégorisation générique des états ("à faire / en cours / terminé")

**Decision**: Utiliser l'endpoint Azure DevOps `GET .../_apis/wit/workitemtypes/{type}/states?api-
version=7.1`, qui renvoie pour chaque type de work item la liste de ses états réels avec une
`category` normalisée (`Proposed`, `InProgress`, `Resolved`, `Completed`, `Removed`) — indépendante
du modèle de processus (Basic, Agile, Scrum, CMMI). Le mapping vers les 3 buckets FR-002 est :
`Proposed` → à faire, `InProgress`/`Resolved` → en cours, `Completed` → terminé. Les work items en
état `Removed` sont exclus de tous les comptages (ni "à faire", ni "terminé", ni comptés dans le
total planifié pour SC/US3) — un work item annulé n'est pas un engagement de sprint non tenu.

**Rationale**: Coder en dur les noms d'états (`New`, `Active`, `Closed`...) casserait pour toute
équipe utilisant un modèle de processus différent (Scrum: `Approved`/`Committed`/`Done`, CMMI:
`Proposed`/`Active`/`Resolved`/`Closed`...). La `category` est le mécanisme officiel d'Azure DevOps
pour ce regroupement générique, déjà exposé par l'API que specs/005-azure-devops-boards utilise.

**Alternatives considered**: Mapping statique par nom d'état — rejeté car fragile et
silencieusement incorrect pour toute équipe hors du process template implicitement supposé.
Extension Analytics OData d'Azure DevOps (expose directement une catégorie d'état par ligne) —
rejeté : c'est un service séparé, pas garanti activé sur tous les projets/organisations, alors que
l'API WIT classique (déjà utilisée par `AzureDevOpsClient`) l'est toujours.

## 2. Distinction Task / User Story / Autres (FR-003, FR-004)

**Decision**: Comparer directement `System.WorkItemType` aux valeurs littérales `"Task"` et `"User
Story"` (noms des types du process template Agile, déjà celui implicitement visé par la formulation
de la feature) ; tout autre type (Bug, Feature, Product Backlog Item d'un process Scrum, etc.)
tombe dans le bucket "Autres" (FR-004).

**Rationale**: La spec ne demande explicitement que Task et User Story ; généraliser à tous les
équivalents "backlog item" par process template (Product Backlog Item, Requirement...) sans qu'on
en ait besoin serait de la sur-ingénierie (Constitution Principe VI). Le bucket "Autres" absorbe
ces cas sans les faire disparaître silencieusement (FR-004), et reste extensible plus tard si une
équipe a besoin d'un mapping configurable.

**Alternatives considered**: Mapping configurable par équipe des types équivalents à "User Story" —
rejeté pour ce MVP, non demandé, ajoute de la configuration sans besoin exprimé.

## 3. Récupération des données work item (extension du client existant)

**Decision**: Étendre `AzureDevOpsClient.ListerWorkItemsAsync` (déjà utilisé par l'import de
specs/005-azure-devops-boards) pour demander aussi les champs `System.WorkItemType` et
`System.State` (en plus de `System.Title` déjà demandé), et enrichir `AzureDevOpsWorkItemSummary`
avec `Type` et `Etat`. Un nouveau point d'entrée `AzureDevOpsClient.ObtenirEtatsAsync(organisation,
projet, pat, type)` expose le mapping état→catégorie par type de work item.

**Rationale**: Le WIQL + batch details est déjà en place et récupère les mêmes work items de
l'Iteration ; ajouter deux champs à la même requête évite un second aller-retour et duplique zéro
logique. L'import de post-its (US3 de specs/005) continue de fonctionner à l'identique — il ignore
simplement les champs additionnels qu'il n'utilise pas.

**Alternatives considered**: Une méthode entièrement séparée avec sa propre requête WIQL — rejeté,
duplique la logique de résolution des IDs par Iteration déjà écrite et testée dans
`ListerWorkItemsAsync`.

## 4. Emplacement du calcul et persistance

**Decision**: Un service applicatif calcule les statistiques à la demande à partir des appels
Azure DevOps ci-dessus, sans rien persister en base ScrumMaster — cohérent avec le Key Entities de
spec.md ("n'est pas une donnée persistée"). Exposé via un nouvel endpoint REST `GET
/api/boards/{boardId}/point-de-sprint`, plutôt qu'une méthode de hub SignalR : c'est une
consultation personnelle à la demande, sans effet de bord à diffuser aux autres participants
(contrairement à l'import/export de specs/005, qui modifient l'état partagé du board).

**Rationale**: Suit le pattern REST déjà utilisé pour `GET /api/boards/{boardId}` (lecture d'état à
la demande) plutôt que le pattern hub réservé aux actions qui mutent et diffusent un état partagé.

## 5. Contrôle d'accès

**Decision**: Le nouvel endpoint exige un `participantId` valide sur le board (facilitateur ou
participant, cohérent avec l'Assumption de spec.md que la consultation est ouverte à tous), sans
exiger que le board soit encore actif (`BoardClosureGuard.EnsureActif` n'est PAS appliqué à ce
endpoint, contrairement à l'import/export) — cohérent avec FR-001 ("à tout moment tant que le board
existe").

**Rationale**: FR-001 exige explicitement l'indépendance vis-à-vis du statut du board ; réutiliser
le garde-fou de fermeture existant contredirait directement cette exigence.
