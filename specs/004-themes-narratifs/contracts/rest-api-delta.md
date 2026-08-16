# Contract Delta: API REST (extension de specs/001-retro-board-base)

Aucun nouvel endpoint. Les endpoints suivants (voir
`specs/001-retro-board-base/contracts/rest-api.md`) gagnent deux champs facultatifs, `icone` et
`contexte`, sur chaque objet représentant un thème.

## `GET /api/themes`

**Réponse 200** :
```json
[
  {
    "id": "guid",
    "nom": "Le père Noël ou le père fouettard",
    "icone": "🎅",
    "contexte": "Chaque membre a reçu un cadeau ou un gage selon...",
    "colonnes": ["Cadeaux", "Gages"]
  },
  { "id": "guid", "nom": "Start / Stop / Continue", "icone": null, "contexte": null, "colonnes": ["Start", "Stop", "Continue"] }
]
```

## `POST /api/boards`

**Requête** (extension de `themePersonnalise`) :
```json
{
  "areaPath": "Krypton",
  "iteration": "Sprint-138",
  "themeId": "guid-optionnel",
  "themePersonnalise": {
    "nom": "Les 3 petits cochons",
    "icone": "🐷",
    "contexte": "Qu'est-ce qui a tenu solide ce sprint, et qu'est-ce qui s'est envolé ?",
    "colonnes": ["Paille", "Bois", "Briques"]
  },
  "maxVotesParParticipant": 3,
  "nomAffiche": "Alex"
}
```
Rejeté (400) si `themePersonnalise.contexte` dépasse 500 caractères (FR-008).

## `GET /api/boards/{boardId}?asParticipantId={participantId}`

**Réponse 200** (extension de `theme`) :
```json
{
  "theme": {
    "id": "guid",
    "nom": "Les 3 petits cochons",
    "icone": "🐷",
    "contexte": "Qu'est-ce qui a tenu solide ce sprint, et qu'est-ce qui s'est envolé ?"
  }
}
```
`icone` et `contexte` valent `null` lorsqu'ils n'ont pas été renseignés pour ce thème (FR-005).
