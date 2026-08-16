# Contract: API REST

Utilisée pour le cycle de vie du board (création, chargement initial/reconnexion, jonction de
participants) et la liste des thèmes prédéfinis. Les mutations de contenu en temps réel (post-its,
votes, thème, clôture) passent par le hub SignalR — voir [realtime-hub.md](./realtime-hub.md).

## `GET /api/themes`

Liste les thèmes prédéfinis proposés à la création d'un board (FR-002, FR-003).

**Réponse 200** :
```json
[
  { "id": "guid", "nom": "Start / Stop / Continue", "colonnes": ["Start", "Stop", "Continue"] },
  { "id": "guid", "nom": "Mad / Sad / Glad", "colonnes": ["Mad", "Sad", "Glad"] }
]
```

## `POST /api/boards`

Crée un board et son facilitateur (FR-001, FR-013, FR-017). Le créateur devient automatiquement
`Facilitateur`.

**Requête** :
```json
{
  "areaPath": "Krypton",
  "iteration": "Sprint-138",
  "themeId": "guid-optionnel",
  "themePersonnalise": { "nom": "Mon thème", "colonnes": ["A", "B"] },
  "maxVotesParParticipant": 3,
  "nomAffiche": "Alex"
}
```
`themeId` XOR `themePersonnalise` — l'un des deux est requis. Si aucun n'est fourni, le thème par
défaut du système est appliqué (FR-002). Rejeté (400) si `themePersonnalise.colonnes` est vide
(FR-015), ou si `areaPath`/`iteration`/`nomAffiche` sont vides (FR-017).

**Réponse 201** :
```json
{
  "boardId": "guid",
  "participantId": "guid",
  "role": "Facilitateur",
  "lienAcces": "/board/{boardId}"
}
```

## `POST /api/boards/{boardId}/participants`

Rejoint un board existant (FR-012 — le lien seul suffit). Refusé (404) si le board n'existe pas.

**Requête** :
```json
{ "nomAffiche": "Sam" }
```

**Réponse 201** :
```json
{ "participantId": "guid", "role": "Participant" }
```

## `GET /api/boards/{boardId}?asParticipantId={participantId}`

Charge l'état complet du board — utilisé au premier chargement et lors d'une reconnexion (User
Story 2, scénario 3). Refusé (404) si le board n'existe pas. Le paramètre de requête
`asParticipantId` est optionnel ; lorsqu'il est fourni (toujours par le client, avec son propre
`participantId`), la réponse est personnalisée avec `mesVotesRestants` et, pour chaque post-it,
`voteDuParticipant` — sans exposer qui d'autre a voté pour quoi.

**Réponse 200** :
```json
{
  "boardId": "guid",
  "areaPath": "Krypton",
  "iteration": "Sprint-138",
  "statut": "Actif",
  "maxVotesParParticipant": 3,
  "mesVotesRestants": 2,
  "theme": { "id": "guid", "nom": "Start / Stop / Continue" },
  "colonnes": [{ "id": "guid", "intitule": "Start", "ordre": 0 }],
  "postIts": [
    {
      "id": "guid",
      "colonneId": "guid",
      "texte": "…",
      "auteur": "Alex",
      "auteurParticipantId": "guid",
      "nombreVotes": 2,
      "voteDuParticipant": true
    }
  ],
  "participants": [{ "id": "guid", "nomAffiche": "Alex", "role": "Facilitateur" }]
}
```

`mesVotesRestants` vaut `null` et `voteDuParticipant` vaut `false` pour chaque post-it lorsque
`asParticipantId` est omis.
