# Data Model: Thèmes de Rétrospective Narratifs

Extension de `specs/001-retro-board-base/data-model.md` — seul `Thème` est modifié, aucune autre
entité n'est touchée.

## Thème (extension)

| Champ | Type | Contraintes |
|-------|------|-------------|
| Icone | string, nullable | Facultatif ; longueur maximale courte (50 caractères) — un emoji ou un court texte (voir `research.md#1`) |
| Contexte | string, nullable | Facultatif ; longueur maximale 500 caractères (FR-008) |

Tous les autres champs (`Id`, `Nom`, `EstPredefini`, `EstParDefaut`, `Colonnes`) restent inchangés
par rapport à `specs/001-retro-board-base/data-model.md`.

**Règle de validation** : à la résolution d'un thème personnalisé (`ThemePersonnaliseDto`), un
`Contexte` dépassant 500 caractères est rejeté (FR-008, 400). Aucune contrainte de format sur
`Icone` au-delà de la longueur maximale.

**Propagation** : comme `Nom` et `Colonnes`, `Icone` et `Contexte` sont copiés par
`BoardService.CopyTheme` à chaque résolution de thème (choix d'un thème prédéfini par id, ou
saisie d'un thème personnalisé) — que ce soit à la création d'un board ou lors d'un changement de
thème en cours de session (`ChangeTheme`, specs/001-retro-board-base User Story 4).

## Relations (inchangées)

```text
Équipe (1) ──< (N) Board
Thème (1) ──< (N) Colonne
Board (1) ── (1) Thème   [référence, copié à chaque résolution — Icone/Contexte inclus]
```
