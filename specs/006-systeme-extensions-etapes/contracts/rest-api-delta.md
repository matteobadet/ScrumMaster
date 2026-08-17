# Contract Delta: API REST (restructuration de specs/001-retro-board-base)

## `POST /api/boards` (requête étendue)

`themeId`/`themePersonnalise` restent acceptés mais deviennent **optionnels** : ils décrivent
implicitement une séquence à une seule étape "Colonnes et post-its" (rétrocompatibilité, FR-014).
Un nouveau champ optionnel `etapes` permet de composer une séquence explicite :

```json
{
  "areaPath": "Krypton",
  "iteration": "Sprint-138",
  "etapes": [
    { "type": "MiniJeu", "miniJeuCatalogueId": "guid" },
    { "type": "ColonnesEtPostIts", "themeId": "guid-optionnel", "themePersonnalise": { "...": "..." } },
    { "type": "PollPersonnalise", "question": "On garde la mêlée du matin ?", "options": ["Oui", "Non"] }
  ],
  "maxVotesParParticipant": 3,
  "nomAffiche": "Alex"
}
```
Si `etapes` est omis, le comportement est celui d'aujourd'hui : une séquence à une seule étape
"Colonnes et post-its", construite depuis `themeId`/`themePersonnalise` (FR-014). Si `etapes` est
fourni, il DOIT contenir au moins un élément (FR-002) ; chaque étape de type `PollPersonnalise`
DOIT porter au moins deux `options` (FR-010).

## `GET /api/boards/{boardId}` (réponse restructurée)

Les champs `theme`/`colonnes`/`postIts` au premier niveau disparaissent, remplacés par `etapes` —
**toutes** les étapes de la séquence sont renvoyées (pas seulement l'active), pour que les étapes
déjà terminées restent consultables en lecture seule (FR-007) :

```json
{
  "boardId": "guid",
  "areaPath": "Krypton",
  "iteration": "Sprint-138",
  "statut": "Actif",
  "maxVotesParParticipant": 3,
  "etapes": [
    {
      "id": "guid",
      "type": "MiniJeu",
      "ordre": 0,
      "statut": "Terminee",
      "miniJeu": { "id": "guid", "nom": "Météo d'équipe", "typeInterne": "meteo-equipe" },
      "reponsesMeteo": [{ "participantId": "guid", "nomAffiche": "Alex", "humeur": "Ensoleille" }]
    },
    {
      "id": "guid",
      "type": "ColonnesEtPostIts",
      "ordre": 1,
      "statut": "Active",
      "theme": { "id": "guid", "nom": "Start / Stop / Continue", "icone": null, "contexte": null },
      "colonnes": [{ "id": "guid", "intitule": "Start", "ordre": 0 }],
      "postIts": [{ "id": "guid", "colonneId": "guid", "texte": "…", "...": "..." }],
      "mesVotesRestants": 2
    },
    {
      "id": "guid",
      "type": "PollPersonnalise",
      "ordre": 2,
      "statut": "AVenir",
      "question": "On garde la mêlée du matin ?",
      "options": [{ "id": "guid", "texte": "Oui", "decompte": 0 }, { "id": "guid", "texte": "Non", "decompte": 0 }],
      "maReponseOptionId": null
    }
  ],
  "participants": [{ "id": "guid", "nomAffiche": "Alex", "role": "Facilitateur" }]
}
```
`mesVotesRestants` (par étape "Colonnes et post-its") et `maReponseOptionId` (par étape "Poll
personnalisé") ne sont personnalisés que si `asParticipantId` est fourni, comme aujourd'hui pour
`mesVotesRestants`/`voteDuParticipant` (specs/001-retro-board-base). Pour une étape `AVenir`, le
détail de son contenu (colonnes/post-its, options, réponses) reste renvoyé — seule l'interaction
est refusée côté serveur (FR-004) tant qu'elle n'est pas `Active`.
