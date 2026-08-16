# Data Model: Intégration Azure DevOps Boards

Extension de `specs/001-retro-board-base/data-model.md` — une nouvelle entité et deux attributs
additionnels sur `Post-it`.

## Configuration Azure DevOps de l'équipe

| Champ | Type | Contraintes |
|-------|------|-------------|
| AreaPath | string | Clé primaire ; clé étrangère → Équipe (specs/001-retro-board-base) — une seule configuration active par équipe (FR-001) |
| Organisation | string | Non vide |
| Projet | string | Non vide |
| PatChiffre | string | PAT chiffré via ASP.NET Core Data Protection (`research.md#2`) — jamais exposé en clair (FR-002) |
| DateConfiguration | timestamp | Mise à jour à chaque remplacement du PAT (FR-004) |

**Règle de validation** : l'enregistrement/remplacement DOIT être précédé d'un appel réel à Azure
DevOps validant l'Organisation/Projet/PAT ensemble (FR-003) — rejeté (400) si l'appel échoue
(PAT invalide, projet introuvable, permissions insuffisantes).

**Accès** : aucune restriction de rôle pour lire ou écrire cette configuration (clarification de
`spec.md`) — cohérent avec `Equipe.TeamsChannelId` (specs/002-poll-utilite-reunion).

## Post-it (extension)

| Champ | Type | Contraintes |
|-------|------|-------------|
| WorkItemSourceId | int, nullable | Renseigné lorsque le post-it a été créé par import (US3) — identifiant du work item Azure DevOps d'origine, sert à ne pas réimporter deux fois le même work item (dédoublonnage symétrique à FR-010) |
| WorkItemExporteId | int, nullable | Renseigné lorsque le post-it a été exporté (US4) — identifiant du work item Azure DevOps créé ; sa présence empêche un second export (FR-010) |

Tous les autres champs (`Id`, `BoardId`, `ColonneId`, `Texte`, `AuteurParticipantId`,
`DateCreation`, `DateModification`, `Votes`) restent inchangés par rapport à
`specs/001-retro-board-base/data-model.md`.

## Relations (résumé)

```text
Équipe (1) ── (0..1) Configuration Azure DevOps   [clé partagée AreaPath]
Board (1) ──< (N) Post-it   [inchangé — WorkItemSourceId/WorkItemExporteId sont des attributs simples]
```

## Règles de validation issues des Functional Requirements

- FR-003/FR-004 : toute configuration/remplacement de PAT nécessite un appel de validation réussi
  contre Azure DevOps avant d'être persisté.
- FR-008 : l'import ne crée pas de nouveau post-it pour un work item déjà présent sur le board
  (`WorkItemSourceId` déjà enregistré pour ce `BoardId`).
- FR-010 : l'export est refusé si `PostIt.WorkItemExporteId` est déjà renseigné.
- FR-011 : import et export ne sont acceptés que si l'appelant est le `Participant` avec
  `Role = Facilitateur` du board concerné (même contrôle que `ChangeTheme`/`CloseBoard`,
  specs/001-retro-board-base contracts/realtime-hub.md).
