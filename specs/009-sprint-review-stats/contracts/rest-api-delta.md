# Delta REST API : Point de sprint

Un seul nouvel endpoint, en lecture seule (FR-009). Aucun endpoint existant n'est modifié.

## `GET /api/boards/{boardId}/point-de-sprint`

**Query**: `asParticipantId` (Guid, requis) — participant appelant, doit exister sur ce board (tout
rôle, cohérent avec l'Assumption "visible par tout participant").

**Réponses**:

- `200 OK` avec `PointDeSprintDto` (voir data-model.md) lorsque l'équipe du board a un accès Azure
  DevOps configuré et que la récupération a réussi.
- `404 Not Found` si le board ou le participant n'existe pas (cohérent avec les autres endpoints
  `boards/{boardId}/...`).
- `400 Bad Request` si l'équipe du board n'a pas d'accès Azure DevOps configuré (FR-006) — message
  explicite invitant à configurer l'accès.
- `502 Bad Gateway` si la récupération depuis Azure DevOps échoue (organisation injoignable, PAT
  expiré) — réutilise `DomainUpstreamException`, déjà mappé par le middleware d'exceptions existant
  (cohérent avec specs/005-azure-devops-boards).

**Payload de succès** (`PointDeSprintDto`) :

```json
{
  "iteration": "MonProjet\\Sprint 12",
  "repartitionParType": [
    { "type": "Task", "aFaire": 3, "enCours": 2, "termine": 5 },
    { "type": "UserStory", "aFaire": 1, "enCours": 1, "termine": 2 }
  ],
  "totalPlanifie": 14,
  "totalTermine": 7
}
```

Un Iteration sans work item (US1 Acceptance Scenario 2) renvoie `200 OK` avec
`repartitionParType: []`, `totalPlanifie: 0`, `totalTermine: 0` — le front affiche l'état vide
explicite (FR-008), ce n'est pas une erreur serveur.
