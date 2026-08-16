# Contract Delta: API REST (extension de specs/001-retro-board-base)

Nouveaux endpoints, hors session de board (voir `research.md#6`).

## `PUT /api/equipes/{areaPath}/azure-devops-config`

Configure ou remplace l'accès Azure DevOps de l'équipe (US1, FR-001, FR-003, FR-004). Rejeté (404)
si l'`Équipe` n'existe pas encore (doit déjà exister via un premier board, cohérent avec
`associer` de specs/002-poll-utilite-reunion).

**Requête** :
```json
{ "organisation": "mon-organisation", "projet": "MonProjet", "pat": "xxxxxxxxxxxx" }
```

**Comportement** :
- Le système appelle Azure DevOps (`GET .../_apis/projects/{projet}`, authentifié par le `pat`
  fourni) pour valider la combinaison avant tout enregistrement (FR-003).
- Échec de validation → 400, message d'erreur explicite, **le PAT n'apparaît jamais dans la
  réponse** (FR-002).
- Succès → la configuration est enregistrée (PAT chiffré, `research.md#2`), remplaçant toute
  configuration existante pour cette équipe (FR-004).

**Réponse 200** :
```json
{ "areaPath": "Krypton", "organisation": "mon-organisation", "projet": "MonProjet" }
```
(le PAT n'est jamais renvoyé, ni en clair ni chiffré)

## `GET /api/equipes/avec-azure-devops`

Liste les équipes déjà configurées, pour alimenter la sélection guidée de l'Area Path à la
création d'un board (US2, `research.md#3`).

**Réponse 200** :
```json
[{ "areaPath": "Krypton" }, { "areaPath": "AutreEquipe" }]
```

## `GET /api/equipes/{areaPath}/azure-devops/iterations`

Liste les Iterations réelles d'Azure DevOps pour l'équipe, avec l'indication de l'Iteration en
cours (US2, FR-005a). Rejeté (404) si l'équipe n'a pas de configuration Azure DevOps. Rejeté (502)
si l'appel à Azure DevOps échoue (organisation injoignable, PAT expiré) — le frontend bascule
alors sur la saisie en texte libre (FR-007).

**Réponse 200** :
```json
[
  { "cheminIteration": "MonProjet\\Sprint 137", "enCours": false },
  { "cheminIteration": "MonProjet\\Sprint 138", "enCours": true }
]
```
Au plus une entrée a `enCours: true` (`research.md#4`) ; aucune si la date du jour ne tombe dans
aucune Iteration connue (Edge Cases de `spec.md`).

## `POST /api/boards` (extension, specs/001-retro-board-base)

`themeId`/`themePersonnalise` inchangés. `areaPath` et `iteration` peuvent désormais provenir
soit d'une sélection guidée (valeurs retournées par les deux endpoints ci-dessus), soit d'une
saisie en texte libre (FR-006/FR-007) — le contrat de la requête elle-même ne change pas (toujours
deux chaînes de caractères), seule leur origine (guidée ou libre) diffère côté client.
