# Contract Delta: Hub SignalR (extension de specs/001-retro-board-base, specs/004-themes-narratifs)

Aucune nouvelle méthode de hub, aucun nouvel événement. Seule la forme du payload déjà transporté
par l'événement `ThemeChanged` (méthode `ChangeTheme`, inchangée en signature) est étendue, au
même endroit que `GET /api/boards/{boardId}` (`ColonneDto`, `contracts/rest-api-delta.md`) :

```json
{
  "theme": { "id": "guid", "nom": "Mon thème", "icone": "🎨", "contexte": null },
  "colonnes": [
    { "id": "guid", "intitule": "Start", "ordre": 0, "couleur": "#d4f5d4", "urlIllustration": null }
  ]
}
```

`ChangeTheme(boardId, themeId, themePersonnalise)` accepte en entrée la même forme étendue de
`themePersonnalise.colonnes` que `POST /api/boards` (`rest-api-delta.md`), avec la même validation
(HTTPS obligatoire pour une URL d'illustration non vide).
