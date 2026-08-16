# Data Model: Rappel de Réunion Teams

Extension de `specs/002-poll-utilite-reunion/data-model.md` — une nouvelle entité, aucune autre
entité modifiée.

## Rappel envoyé

| Champ | Type | Contraintes |
|-------|------|-------------|
| Id | Guid | Clé primaire |
| AreaPath | string | Clé étrangère → Équipe, non vide |
| TypeReunion | enum `Melee` \| `Retrospective` | Réutilise l'enum de specs/002-poll-utilite-reunion |
| Date | date (sans heure) | Occurrence du jour ; identifie, avec AreaPath et TypeReunion, une occurrence unique (FR-008) |
| DateEnvoi | timestamp | Non modifiable après création ; automatique ou manuel (indifférencié, voir Assumptions de spec.md) |

**Contrainte d'unicité** : `(AreaPath, TypeReunion, Date)` — un seul rappel par équipe, par type de
réunion et par jour (FR-008), même pattern que `PollUtilite`
(specs/002-poll-utilite-reunion/data-model.md).

**Règle de validation issue des Functional Requirements** :
- FR-003/FR-004 : toute commande de rappel manuel nécessite que l'`Équipe` associée au channel
  courant (`TeamsChannelId`) existe — sinon rejet.
- FR-008 : une tentative d'enregistrement en violation de la contrainte d'unicité est traitée
  différemment selon l'origine — silencieuse pour un rappel automatique (aucun message d'erreur,
  pas de déclencheur humain à informer), rejetée avec message explicite pour un rappel manuel.

## Relations (résumé)

```text
Équipe (1) ──< (N) Rappel envoyé
```

Aucune relation directe avec `Poll d'utilité` : un rappel automatique est déclenché par la
clôture d'un poll (FR-001) mais n'y reste pas rattaché en base — le dédoublonnage se fait
uniquement sur `(AreaPath, TypeReunion, Date)`, cohérent avec le fait qu'un rappel manuel (US2)
peut exister sans aucun poll.
