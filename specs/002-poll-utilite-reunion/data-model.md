# Data Model: Poll d'Utilité de Réunion

## Équipe (extension de specs/001-retro-board-base)

| Champ | Type | Contraintes |
|-------|------|-------------|
| TeamsChannelId | string, nullable | Identifiant de la conversation/channel Teams associé (FR-001, FR-002) ; `null` tant qu'aucune association n'a été réalisée |

Tous les autres champs (`AreaPath` en clé primaire) restent inchangés par rapport à
specs/001-retro-board-base/data-model.md.

## Poll d'utilité

| Champ | Type | Contraintes |
|-------|------|-------------|
| Id | Guid | Clé primaire |
| AreaPath | string | Clé étrangère → Équipe, non vide |
| TypeReunion | enum `Melee` \| `Retrospective` | Non vide (FR-005) |
| Date | date (sans heure) | Occurrence du jour ; identifie, avec AreaPath et TypeReunion, une occurrence unique de réunion (FR-011) |
| Statut | enum `Ouvert` \| `Clos` | Transition unique `Ouvert → Clos`, déclenchée par la commande de clôture (FR-004) ; pas de réouverture |
| DateCreation | timestamp | Non modifiable après création |
| DateCloture | timestamp, nullable | Renseigné à la clôture |

**Contrainte d'unicité** : `(AreaPath, TypeReunion, Date)` — un seul poll par équipe, par type de
réunion et par jour (Assumptions de `spec.md`).

**Résultat** : non stocké — dérivé à la lecture des votes selon la règle FR-009 (réunion maintenue
dès qu'au moins un vote "Utile" est présent parmi les votes du poll).

## Vote d'utilité

| Champ | Type | Contraintes |
|-------|------|-------------|
| PollId | Guid | Clé étrangère → Poll d'utilité (clé composite avec TeamsUserId) |
| TeamsUserId | string | Identifiant Teams (AAD Object Id) du votant, tel que fourni par l'activité Bot Framework |
| NomAffiche | string | Nom affiché Teams au moment du vote (FR-012 — affichage non anonyme) |
| Reponse | enum `Utile` \| `PasNecessaire` | Non vide (FR-006) |
| DateVote | timestamp | Mise à jour à chaque changement de vote (FR-007) |

**Contrainte d'unicité** : `(PollId, TeamsUserId)` — un seul vote actif par membre et par poll ;
un nouveau vote du même membre remplace le précédent plutôt que d'en créer un second (FR-007).

## Relations (résumé)

```text
Équipe (1) ── (0..1) TeamsChannelId          [attribut simple, pas une entité séparée]
Équipe (1) ──< (N) Poll d'utilité
Poll d'utilité (1) ──< (N) Vote d'utilité
```

## Règles de validation issues des Functional Requirements

- FR-003/FR-004 : toute commande de déclenchement ou de clôture nécessite que l'`Équipe` associée
  au channel courant (`TeamsChannelId`) existe — sinon rejet (voir Edge Cases de `spec.md`).
  L'`Équipe` elle-même doit déjà exister (créée via specs/001-retro-board-base) ; cette feature ne
  crée pas d'équipe.
- FR-007/FR-008 : un vote sur un poll `Clos` est rejeté.
- FR-009 : le calcul du résultat lit l'ensemble des `Vote d'utilité` d'un `Poll d'utilité` donné —
  "Utile" si au moins un vote `Utile` existe, "Pas nécessaire" sinon (et seulement s'il existe au
  moins un vote au total — un poll clos sans aucun vote retient "réunion maintenue" par défaut,
  cf. Assumptions).
