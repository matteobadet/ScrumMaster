# Contract Delta: API REST (extension de specs/006-systeme-extensions-etapes)

## `POST /api/boards` (requête étendue — composition d'une étape ROTI)

`etapes[].rotiPersonnalisations` est un nouveau champ optionnel, valide uniquement quand
`etapes[].type == "MiniJeu"` et que `miniJeuCatalogueId` référence le mini-jeu ROTI :

```json
{
  "etapes": [
    {
      "type": "MiniJeu",
      "miniJeuCatalogueId": "guid-du-roti",
      "rotiPersonnalisations": [
        { "niveau": "PerteDeTemps", "urlIllustration": "https://example.com/perte-de-temps.png" },
        { "niveau": "TresRentable", "urlIllustration": "https://example.com/tres-rentable.png" }
      ]
    }
  ]
}
```

Seuls les niveaux à personnaliser sont fournis — les autres gardent le visuel par défaut. Une
`rotiPersonnalisations` fournie pour un mini-jeu autre que ROTI, ou une `urlIllustration`
non-HTTPS ou dépassant 2048 caractères, est refusée avec `400 Bad Request` (FR-005 à FR-007).

## `GET /api/boards/{boardId}` (réponse étendue — étape MiniJeu de type ROTI)

Pour une étape dont `miniJeu.typeInterne == "roti"`, `EtapeDto` porte désormais :

```json
{
  "type": "MiniJeu",
  "miniJeu": { "id": "guid", "nom": "ROTI", "typeInterne": "roti" },
  "reponsesRoti": [
    { "participantId": "guid", "nomAffiche": "Alex", "niveau": "TresRentable" }
  ],
  "monNiveauRoti": "TresRentable",
  "visuelsRoti": [
    { "niveau": "PerteDeTemps", "urlIllustration": "https://example.com/perte-de-temps.png" }
  ]
}
```

`visuelsRoti` ne contient que les niveaux personnalisés (liste creuse) — un niveau absent affiche
le visuel par défaut côté client. `monNiveauRoti` n'est renseigné que si `asParticipantId` est
fourni, comme `monHumeur` pour "Météo d'équipe".

`GET /api/mini-jeux` renvoie désormais deux entrées (`Météo d'équipe`, `ROTI`) — forme de réponse
inchangée.
