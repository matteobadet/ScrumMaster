# Delta REST API : Historique des boards

Un seul nouvel endpoint, en lecture seule. Aucun endpoint existant n'est modifié.

## `GET /api/equipes/{areaPath}/boards`

**Réponses**:

- `200 OK` avec une liste de `BoardSummaireDto` (voir data-model.md), triée par `DateCreation`
  décroissante (FR-003). Une équipe inconnue ou sans board renvoie `200 OK` avec une liste vide
  (FR-005) — pas de `404`, cohérent avec l'absence d'authentification (on ne distingue pas "équipe
  inconnue" de "équipe sans historique").

**Payload de succès**:

```json
[
  { "id": "3ba6e7af-9711-4ec5-b34d-363efddf0966", "iteration": "Sprint-12", "statut": "Cloture", "dateCreation": "2026-08-10T09:00:00Z" },
  { "id": "98141549-f028-495e-b214-662c3903cba0", "iteration": "Sprint-11", "statut": "Cloture", "dateCreation": "2026-07-27T09:00:00Z" }
]
```
