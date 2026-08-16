# Contract Delta: Hub temps réel (extension de specs/001-retro-board-base)

Deux nouvelles méthodes sur `RetroBoardHub` (voir
`specs/001-retro-board-base/contracts/realtime-hub.md`), même modèle d'autorisation et de rejet
que `ChangeTheme`/`CloseBoard`.

## `ImportWorkItems` (méthode Client → Serveur, US3)

| Méthode | Paramètres | Autorisation | Rejet si |
|---------|-----------|---------------|----------|
| `ImportWorkItems` | `boardId` | Facilitateur uniquement (FR-011) | appelant non-facilitateur ; board `Cloture` ; équipe sans configuration Azure DevOps |

**Comportement** : interroge Azure DevOps par WIQL sur `[System.IterationPath] = Board.Iteration`
(`research.md#5`), crée un post-it par work item non déjà importé (`WorkItemSourceId`, FR-008),
dans la première colonne du thème du board. Diffuse un événement `PostItAdded` par post-it créé
(réutilise l'événement existant de specs/001-retro-board-base — aucun nouveau format de payload).
Si aucun work item n'est trouvé (Iteration texte libre ou vide), aucun post-it n'est créé, aucune
erreur n'est levée (Edge Cases de `spec.md`).

## `ExportPostIt` (méthode Client → Serveur, US4)

| Méthode | Paramètres | Autorisation | Rejet si |
|---------|-----------|---------------|----------|
| `ExportPostIt` | `boardId`, `postItId` | Facilitateur uniquement (FR-011) | appelant non-facilitateur ; board `Cloture` ; équipe sans configuration Azure DevOps ; post-it déjà exporté (FR-010) |

**Comportement** : crée un work item de type "Task" dans Azure DevOps avec `Texte` du post-it
comme titre (Assumptions de `spec.md`), enregistre `WorkItemExporteId` sur le post-it, diffuse
`PostItExported` (nouvel événement, ci-dessous).

## `PostItExported` (nouvel événement Serveur → Groupe)

| Événement | Payload | Déclenché par |
|-----------|---------|----------------|
| `PostItExported` | `{ postItId, workItemId }` | `ExportPostIt` |

Le client marque visuellement le post-it comme exporté et désactive l'action d'export pour ce
post-it, dès réception (cohérent avec le comportement déjà appliqué localement par l'appelant lors
de l'appel réussi).
