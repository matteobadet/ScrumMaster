# Feature Specification: Mini-jeu ROTI

**Feature Branch**: `008-roti-mini-jeu`

**Created**: 2026-08-17

**Status**: Draft

**Input**: User description: "Nouveau mini-jeu 'ROTI' (Return On Time Invested) dans le catalogue
de mini-jeux du système d'extensions/étapes (specs/006-systeme-extensions-etapes), aux côtés du
mini-jeu déjà existant 'Météo d'équipe'. Le ROTI est une activité de clôture de rétrospective
classique où chaque participant évalue si le temps investi dans la réunion en valait la peine,
généralement sur une échelle de quelques niveaux (ex: du 'perte de temps' au 'très rentable'). Le
facilitateur doit pouvoir choisir, comme pour les illustrations de colonnes
(specs/007-themes-visuels-colonnes), soit une image personnalisée par niveau (URL fournie par le
facilitateur), soit un visuel prédéfini fourni par l'équipe projet pour chaque niveau de l'échelle.
Contexte projet : ScrumMaster a déjà un système d'étapes de type Mini-jeu (specs/006) avec un
mécanisme de réponse par participant (upsert, modifiable tant que l'étape est active) et un
catalogue de mini-jeux prédéfinis stocké en base ; ce nouveau mini-jeu doit s'insérer dans ce même
mécanisme (RepondreMiniJeu, catalogue MiniJeuxCatalogue) plutôt que d'en créer un nouveau."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Évaluer le retour sur temps investi avec le visuel par défaut (Priority: P1)

Le facilitateur insère une étape "ROTI" dans sa séquence, sans rien configurer ; chaque participant
évalue, une fois l'étape active, si le temps investi dans la rétrospective en valait la peine, sur
une échelle à plusieurs niveaux illustrée par un visuel fourni par défaut.

**Why this priority**: C'est le cœur de la capacité — sans elle, rien d'autre n'a de sens. Doit
être utilisable immédiatement, sans aucune configuration, comme le mini-jeu "Météo d'équipe" déjà
existant.

**Independent Test**: Peut être testé en insérant une étape "ROTI" dans une séquence sans aucune
configuration, en répondant depuis plusieurs comptes participants une fois l'étape active, et en
vérifiant que chaque réponse est prise en compte et visible par tous.

**Acceptance Scenarios**:

1. **Given** la composition d'une séquence d'étapes, **When** le facilitateur ajoute une étape de
   type "ROTI" sans personnalisation, **Then** cette étape apparaît dans la séquence avec le
   visuel par défaut sur chacun de ses niveaux.
2. **Given** une étape ROTI active, **When** un participant choisit un niveau de l'échelle,
   **Then** sa réponse est enregistrée et visible par tous les participants.
3. **Given** un participant a déjà répondu à une étape ROTI active, **When** il choisit un autre
   niveau, **Then** sa réponse précédente est remplacée (cohérent avec le mini-jeu "Météo
   d'équipe" déjà existant, specs/006-systeme-extensions-etapes).

---

### User Story 2 - Personnaliser le visuel de l'échelle ROTI (Priority: P2)

Le facilitateur remplace, pour un ou plusieurs niveaux de l'échelle ROTI, le visuel par défaut par
sa propre image (via une URL, comme pour les illustrations de colonnes de thème,
specs/007-themes-visuels-colonnes), pour accorder l'étape à ses propres codes visuels d'équipe.

**Why this priority**: Complète la capacité de base (US1) avec une touche de personnalisation,
mais n'est utile qu'une fois l'étape ROTI elle-même utilisable — un facilitateur pressé peut
toujours s'en passer et garder le visuel par défaut.

**Independent Test**: Peut être testé en composant une étape ROTI avec une image personnalisée sur
au moins un niveau (les autres gardant le visuel par défaut), puis en vérifiant que le niveau
personnalisé affiche l'image fournie tandis que les autres affichent toujours le visuel par
défaut.

**Acceptance Scenarios**:

1. **Given** la composition d'une étape ROTI, **When** le facilitateur fournit une URL d'image
   pour un niveau de l'échelle, **Then** ce niveau affiche cette image à la place du visuel par
   défaut, pour tous les participants.
2. **Given** une étape ROTI dont seuls certains niveaux ont une image personnalisée, **When**
   l'étape devient active, **Then** les niveaux non personnalisés affichent le visuel par défaut,
   sans espace vide ni erreur.
3. **Given** le facilitateur fournit une URL d'image non-HTTPS pour un niveau, **When** il tente de
   composer l'étape, **Then** le système refuse avec un message d'erreur explicite (cohérent avec
   specs/007-themes-visuels-colonnes, FR-009).

---

### Edge Cases

- Que se passe-t-il si un participant tente de répondre à une étape ROTI qui n'est pas (encore ou
  plus) active ? L'interaction est refusée (cohérent avec le mini-jeu "Météo d'équipe" déjà
  existant, specs/006-systeme-extensions-etapes).
- Que se passe-t-il si une étape ROTI se termine sans qu'aucun participant n'ait répondu ? L'étape
  se clôt normalement, aucune réponse n'est affichée pour aucun niveau (cohérent avec le
  comportement déjà retenu pour le poll personnalisé, specs/006-systeme-extensions-etapes).
- Que se passe-t-il si l'URL d'une image personnalisée devient inaccessible après coup (lien
  cassé) ? Le niveau concerné s'affiche sans image plutôt que de bloquer l'étape ou le board
  (cohérent avec specs/007-themes-visuels-colonnes, FR-010).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT proposer, dans le catalogue de mini-jeux existant (aux côtés de
  "Météo d'équipe"), un mini-jeu "ROTI" permettant à chaque participant d'évaluer si le temps
  investi dans la rétrospective en valait la peine.
- **FR-002**: L'échelle d'évaluation ROTI DOIT comporter plusieurs niveaux ordonnés, du moins
  favorable ("perte de temps") au plus favorable ("très rentable"), chacun illustré par un visuel
  fourni par défaut sans configuration du facilitateur.
- **FR-003**: Pour une étape ROTI active, le système DOIT permettre à chaque participant de choisir
  un niveau de l'échelle, et de modifier son choix tant que l'étape reste active (remplacement, pas
  de doublon — cohérent avec le mini-jeu "Météo d'équipe", specs/006-systeme-extensions-etapes).
- **FR-004**: Le système DOIT afficher, pour une étape ROTI, la réponse de chaque participant
  ayant répondu, visible par tous les participants (cohérent avec le mini-jeu "Météo d'équipe").
- **FR-005**: Le système DOIT permettre au facilitateur de remplacer, pour un ou plusieurs niveaux
  de l'échelle ROTI, le visuel par défaut par une image de son choix, fournie sous forme d'URL vers
  une image déjà hébergée ailleurs (le facilitateur ne téléverse aucun fichier vers ScrumMaster —
  même mécanisme que l'illustration de colonne, specs/007-themes-visuels-colonnes).
- **FR-006**: La personnalisation du visuel DOIT être possible niveau par niveau et rester
  facultative pour chacun — un niveau sans image personnalisée affiche le visuel par défaut, sans
  effet visuel indésirable.
- **FR-007**: Le système DOIT refuser une URL d'image personnalisée qui n'est pas une adresse
  HTTPS syntaxiquement valide, avec un message d'erreur explicite (cohérent avec
  specs/007-themes-visuels-colonnes, FR-009).
- **FR-008**: Le système NE DOIT PAS bloquer l'affichage d'un niveau de l'échelle, ni du reste de
  l'étape, si l'image personnalisée d'un niveau devient inaccessible après coup (cohérent avec
  specs/007-themes-visuels-colonnes, FR-010).

### Key Entities *(include if feature involves data)*

- **Réponse ROTI**: association entre un participant et le niveau d'échelle qu'il a choisi pour une
  étape ROTI donnée ; modifiable tant que l'étape reste active (même forme que la Réponse "Météo
  d'équipe" déjà existante, specs/006-systeme-extensions-etapes).
- **Personnalisation visuelle de niveau ROTI**: association facultative entre un niveau de
  l'échelle d'une étape ROTI précise et l'URL d'une image qui remplace, pour cette étape
  uniquement, le visuel par défaut de ce niveau.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un facilitateur peut insérer une étape ROTI utilisable, sans aucune configuration, en
  moins d'1 minute.
- **SC-002**: Un facilitateur peut personnaliser l'image d'un niveau de l'échelle ROTI en moins
  d'1 minute.
- **SC-003**: 100% des participants voient les réponses ROTI se mettre à jour sans rechargement
  manuel, dans le même délai que les autres interactions temps réel déjà en place
  (specs/001-retro-board-base).

## Assumptions

- L'échelle ROTI comporte 5 niveaux ("Perte de temps", "Peu rentable", "Moyennement rentable",
  "Rentable", "Très rentable"), un nombre usuel pour cette activité de rétrospective ; le contenu
  exact du libellé de chaque niveau relève du plan technique et de l'implémentation, pas de cette
  spécification.
- Le visuel par défaut de chaque niveau (fourni sans configuration, FR-002) est déterminé par
  l'équipe projet au moment de l'implémentation — un emoji ou une icône simple suffit, cohérent
  avec l'approche déjà retenue pour le mini-jeu "Météo d'équipe" (pas de bibliothèque d'images à
  gérer, Constitution Principe VI).
- Comme pour le mini-jeu "Météo d'équipe" déjà existant, la réponse de chaque participant reste
  visible nommément par tous les autres participants — pas d'anonymisation des réponses ROTI dans
  ce périmètre, pour rester cohérent avec le mécanisme déjà en place plutôt que d'introduire une
  nouvelle variante de confidentialité.
- Cette feature réutilise le mécanisme de réponse à un mini-jeu déjà existant
  (specs/006-systeme-extensions-etapes) — pas de nouvel endpoint ni de nouvelle méthode de hub
  générique, seulement l'ajout du contenu ROTI à ce mécanisme.
- La personnalisation visuelle par niveau réutilise le mécanisme d'illustration par URL externe
  déjà validé pour les colonnes de thème (specs/007-themes-visuels-colonnes) — aucune image n'est
  jamais récupérée ou stockée côté serveur, aucune infrastructure de stockage de fichiers n'est
  nécessaire pour cette feature.
