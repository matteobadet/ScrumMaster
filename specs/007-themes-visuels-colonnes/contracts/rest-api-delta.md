# Contract Delta: API REST (extension de specs/001-retro-board-base, specs/004-themes-narratifs)

## `GET /api/themes` (réponse restructurée)

`Colonnes` passe d'un tableau de chaînes à un tableau d'objets, portant chacun sa couleur et son
URL d'illustration facultatives (`research.md#4`) :

```json
[
  {
    "id": "guid",
    "nom": "La rétro du randonneur",
    "icone": "🥾",
    "contexte": "Une expédition en montagne, comme notre sprint...",
    "colonnes": [
      { "intitule": "La corde", "couleur": "#f5e6b8", "urlIllustration": "https://placehold.co/128/f0dd80/4a4220?text=🪢" },
      { "intitule": "Le rocher", "couleur": "#e4e2e8", "urlIllustration": "https://placehold.co/128/cfc9db/1a1625?text=🪨" }
    ]
  }
]
```

Un thème sans couleur/illustration sur ses colonnes (thèmes existants avant cette feature) renvoie
`couleur`/`urlIllustration` à `null` pour chacune — comportement inchangé côté affichage (FR-005).

## `POST /api/boards` (requête étendue)

`themePersonnalise.colonnes` passe de la même façon d'un tableau de chaînes à un tableau d'objets :

```json
{
  "themePersonnalise": {
    "nom": "Mon thème",
    "icone": "🎨",
    "contexte": null,
    "colonnes": [
      { "intitule": "Start", "couleur": "#d4f5d4", "urlIllustration": null },
      { "intitule": "Stop", "couleur": "#f5d4d4", "urlIllustration": "https://example.com/stop.png" }
    ]
  }
}
```

Même changement de forme pour `etapes[].themePersonnalise.colonnes` (composition explicite d'une
séquence, specs/006-systeme-extensions-etapes) — un seul type `ColonneSummaireDto` réutilisé
partout où une liste de colonnes est transportée en entrée.

Une `urlIllustration` non-HTTPS ou dépassant 2048 caractères, ou une `couleur` dépassant 30
caractères, est refusée avec `400 Bad Request` (FR-009, `data-model.md`).

## `GET /api/boards/{boardId}` (réponse étendue)

Chaque `ColonneDto` d'une étape "Colonnes et post-its" gagne `couleur` et `urlIllustration`
(`string | null` chacun), aux côtés de `id`/`intitule`/`ordre` déjà existants :

```json
{
  "etapes": [
    {
      "type": "ColonnesEtPostIts",
      "colonnes": [
        { "id": "guid", "intitule": "La corde", "ordre": 0, "couleur": "#f5e6b8", "urlIllustration": "https://placehold.co/128/f0dd80/4a4220?text=🪢" }
      ]
    }
  ]
}
```
