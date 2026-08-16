# Phase 0 Research: Thèmes de Rétrospective Narratifs

## 1. Format de l'icône de thème

**Decision**: L'icône est stockée comme une chaîne de caractères courte et libre (ex: `"🎅"`),
saisie directement par le facilitateur — aucune bibliothèque d'icônes prédéfinie, aucun système de
sélection/upload d'image.

**Rationale**: Conforme à l'Assumption de `spec.md` et à la Constitution Principe VI (pas de
sur-ingénierie) : un champ texte court couvre le besoin observé (emoji unique) sans construire de
catalogue d'icônes à maintenir.

**Alternatives considered**:
- **Bibliothèque d'icônes prédéfinie (enum ou set curaté)** : plus cohérent visuellement, mais
  nécessite de maintenir un catalogue et un composant de sélection ; rejeté comme
  sur-ingénierie pour ce besoin (un simple emoji suffit à l'usage observé sur le board Figma de
  référence).
- **Upload d'image** : hors périmètre, complexifie le stockage (fichiers binaires) pour une valeur
  ajoutée marginale par rapport à un emoji.

## 2. Portée de l'extension (thème prédéfini et personnalisé)

**Decision**: `Icone` et `Contexte` sont deux nouveaux attributs facultatifs directement sur
l'entité `Theme` existante (`backend/src/ScrumMaster.Api/Models/Theme.cs`), disponibles aussi bien
pour un thème prédéfini (`EstPredefini = true`, saisis lors du seed ou d'une future gestion) que
pour un thème personnalisé (saisis par le facilitateur au moment de la création du board ou d'un
changement de thème — clarification actée dans `spec.md`).

**Rationale**: `Theme` est déjà le point d'extension naturel : `BoardService.ResolveThemeAsync`
copie toujours un `Theme` (prédéfini ou personnalisé) en un nouveau `Theme` via `CopyTheme`
(`backend/src/ScrumMaster.Api/Services/BoardService.cs:224`) — il suffit d'étendre `CopyTheme` pour
propager les deux nouveaux champs en même temps que `Nom` et `Colonnes`, sans introduire de
nouvelle entité ni de nouveau mécanisme de persistance.

**Alternatives considered**:
- **Entité séparée `HabillageTheme`** : introduirait une jointure supplémentaire pour une donnée
  qui n'a de sens qu'attachée à un `Theme` ; rejeté (sur-ingénierie).

## 3. Propagation aux DTOs et au temps réel

**Decision**: `Icone` et `Contexte` sont ajoutés aux DTOs déjà existants qui transportent un thème
— `ThemeSummaryDto` (`GET /api/themes`), `ThemePersonnaliseDto` (saisie du thème personnalisé,
réutilisé par `POST /api/boards` et la méthode SignalR `ChangeTheme`), et `ThemeRefDto` (thème
courant d'un board, retourné par `GET /api/boards/{boardId}` et diffusé par l'événement
`ThemeChanged`) — plutôt que de créer de nouveaux endpoints ou événements.

**Rationale**: Le mécanisme de transport du thème (REST à la création, SignalR au changement en
cours de session) existe déjà intégralement (`contracts/rest-api.md` et `contracts/realtime-hub.md`
de specs/001-retro-board-base) ; cette feature est une extension de champs, pas un nouveau flux.

**Alternatives considered**: Aucune — étendre les contrats existants est la seule option cohérente
avec le principe d'extension additive de `spec.md`.
