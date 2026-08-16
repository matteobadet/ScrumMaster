# Contract Delta: Hub temps réel (extension de specs/001-retro-board-base)

Aucune nouvelle méthode ni nouvel événement. Voir
`specs/001-retro-board-base/contracts/realtime-hub.md`.

## `ChangeTheme` (méthode Client → Serveur)

`themePersonnalise` accepte désormais `icone` et `contexte`, sur le même modèle que
`POST /api/boards` (voir `rest-api-delta.md`). Mêmes règles de rejet côté validation
(`Contexte` > 500 caractères → `HubException`, en plus des rejets déjà existants pour cette
méthode).

## `ThemeChanged` (événement Serveur → Groupe)

**Payload** (extension de `theme`) :
```json
{ "theme": { "id": "guid", "nom": "Les 3 petits cochons", "icone": "🐷", "contexte": "..." }, "colonnes": [...] }
```
Le client met à jour l'icône et le bloc de contexte affichés dès réception de cet événement,
comme il le fait déjà pour le nom du thème et les colonnes (User Story 4 de
specs/001-retro-board-base).
