# Feature Specification: Thèmes de Rétrospective Narratifs

**Feature Branch**: `004-themes-narratifs`

**Created**: 2026-08-16

**Status**: Draft

**Input**: User description: "Enrichissement des thèmes du board de rétrospective
(specs/001-retro-board-base) pour se rapprocher du format de rétro pratiqué par l'équipe (observé
sur un board Figma existant, ex: 'Le père Noël ou le père fouettard', 'La vente aux enchères',
'Les 3 petits cochons'). Chaque thème doit pouvoir porter, en plus du nom et des colonnes déjà
supportés : une icône ou emoji associé au thème, et un bloc de 'Contexte' en texte libre affiché
en introduction du board (avant les colonnes), permettant au facilitateur de planter le décor de
la rétro. Explicitement hors périmètre pour cette feature : pièces jointes/photos sur les
post-its, historique des rétros passées consultable, mini-jeux ou éléments de clôture ludique
(ces derniers relèvent du système d'extensions/plugins, Phase 4 de la constitution,
volontairement hors périmètre). Reste dans le périmètre de la Phase 1 (board de rétrospective) de
la roadmap MVP de la constitution — n'est pas une nouvelle phase."

## Clarifications

### Session 2026-08-16

- Q: L'icône et le contexte doivent-ils être réservés aux thèmes prédéfinis (catalogue), ou
  est-ce que le facilitateur doit aussi pouvoir en saisir pour un thème personnalisé qu'il tape
  lui-même ? → A: Disponibles pour les deux — un thème prédéfini choisi dans le catalogue comme un
  thème personnalisé saisi par le facilitateur (aux côtés du nom et des colonnes) peuvent porter
  une icône et un contexte.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Associer une icône au thème (Priority: P1)

Le facilitateur associe une icône ou un emoji à un thème, affiché à côté du nom du thème dans
l'en-tête du board, pour identifier visuellement l'ambiance de la rétro en un coup d'œil.

**Why this priority**: C'est le changement le plus simple et le plus immédiatement visible ; il ne
dépend d'aucune autre partie de la feature et peut être livré et démontré seul.

**Independent Test**: Peut être testé en créant un board avec un thème (prédéfini ou personnalisé)
auquel une icône est associée, puis en vérifiant qu'elle apparaît dans l'en-tête du board.

**Acceptance Scenarios**:

1. **Given** un facilitateur qui crée un board ou change de thème, **When** il choisit un thème
   prédéfini portant une icône, ou saisit une icône pour son thème personnalisé, **Then** cette
   icône apparaît à côté du nom du thème dans l'en-tête du board.
2. **Given** un thème sans icône associée, **When** un board utilise ce thème, **Then** l'en-tête
   du board affiche uniquement le nom du thème, sans espace vide ni erreur.

---

### User Story 2 - Planter le décor avec un bloc Contexte (Priority: P2)

Le facilitateur rédige un texte de contexte libre pour le thème, affiché en introduction du board
(avant les colonnes), visible par tous les participants dès leur arrivée.

**Why this priority**: Complète la valeur narrative de la feature, mais reste utile même sans
icône (US1) — les deux sont indépendantes l'une de l'autre.

**Independent Test**: Peut être testé en créant un board avec un thème (prédéfini ou personnalisé)
portant un texte de contexte, puis en vérifiant que ce texte apparaît en introduction du board
pour tous les participants.

**Acceptance Scenarios**:

1. **Given** un facilitateur qui crée un board ou change de thème, **When** il choisit un thème
   prédéfini portant un contexte, ou saisit un contexte pour son thème personnalisé, **Then** ce
   texte apparaît en introduction du board, avant les colonnes, visible par tous les participants
   connectés.
2. **Given** un thème sans contexte associé, **When** un board utilise ce thème, **Then** aucun
   bloc de contexte n'est affiché (pas d'espace vide ni de placeholder visible).

---

### Edge Cases

- Que se passe-t-il si le facilitateur change le thème du board en cours de session (mécanisme
  déjà existant, specs/001-retro-board-base User Story 4) ? L'icône et le contexte affichés
  passent immédiatement à ceux du nouveau thème, comme le reste de l'habillage du thème.
- Que se passe-t-il si le texte de contexte saisi dépasse la longueur maximale autorisée ? Le
  système refuse l'enregistrement avec un message indiquant la limite (voir Assumptions).
- Que se passe-t-il pour les thèmes prédéfinis et les boards déjà existants créés avant cette
  feature ? Ils continuent de fonctionner sans icône ni contexte (champs facultatifs, aucune
  migration de contenu requise).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT permettre d'associer une icône ou un emoji à un thème, qu'il
  s'agisse d'un thème prédéfini (catalogue) ou d'un thème personnalisé saisi par le facilitateur
  (aux côtés du nom et des colonnes, au moment de la création du board ou d'un changement de
  thème — specs/001-retro-board-base, User Story 4).
- **FR-002**: Le système DOIT afficher l'icône du thème, si elle existe, à côté du nom du thème
  dans l'en-tête du board.
- **FR-003**: Le système DOIT permettre d'associer un texte de contexte libre à un thème, qu'il
  s'agisse d'un thème prédéfini ou d'un thème personnalisé, selon le même mécanisme que FR-001.
- **FR-004**: Le système DOIT afficher le texte de contexte du thème, s'il existe, en introduction
  du board (avant les colonnes), visible par tous les participants.
- **FR-005**: L'icône et le texte de contexte DOIVENT être facultatifs : un thème reste valide et
  utilisable sans eux, sans effet visuel indésirable (espace vide, placeholder) lorsqu'ils sont
  absents.
- **FR-006**: Chaque choix de thème (à la création du board ou lors d'un changement en cours de
  session, specs/001-retro-board-base User Story 4) DOIT permettre de fournir son icône et son
  contexte au même moment que le nom et les colonnes — il n'existe pas de modification a
  posteriori d'un thème déjà appliqué à un board ; un nouveau choix de thème doit être fait pour
  changer son icône ou son contexte (cohérent avec le fonctionnement actuel du changement de
  thème, qui crée toujours un nouveau thème plutôt que de modifier l'existant).
- **FR-007**: Lorsque le facilitateur change le thème appliqué à un board en cours de session,
  l'icône et le contexte affichés DOIVENT refléter immédiatement le nouveau thème.
- **FR-008**: Le système DOIT refuser un texte de contexte dépassant la longueur maximale
  autorisée (voir Assumptions), avec un message d'erreur explicite.

### Key Entities *(include if feature involves data)*

- **Thème** (extension de specs/001-retro-board-base) : gagne deux attributs facultatifs — une
  icône/emoji, et un texte de contexte libre affiché en introduction du board. Disponibles aussi
  bien pour un thème prédéfini (catalogue) que pour un thème personnalisé saisi par le
  facilitateur. Le nom du thème existant continue de servir à la fois d'identifiant affiché et de
  titre visible dans l'en-tête ; aucun champ supplémentaire n'est introduit pour un "titre
  narratif" distinct.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un facilitateur peut ajouter une icône et un texte de contexte à un thème en moins
  d'1 minute.
- **SC-002**: 100% des participants rejoignant un board dont le thème a un contexte le voient sans
  action supplémentaire de leur part.
- **SC-003**: Les thèmes et boards existants avant cette feature restent utilisables sans
  modification ni erreur après son déploiement.

## Assumptions

- L'icône est saisie librement par le facilitateur sous forme d'un emoji ou d'un court texte (ex:
  "🎅"), sans bibliothèque d'icônes prédéfinie à gérer par le système — cohérent avec le principe
  de ne pas sur-concevoir (Constitution Principe VI).
- Le texte de contexte est limité à 500 caractères, une longueur jugée suffisante pour planter un
  décor de rétro sans nuire à la lisibilité de l'en-tête du board.
- Cette feature n'introduit pas de "titre narratif" distinct du nom de thème déjà existant : le
  nom du thème (ex: "Le père Noël ou le père fouettard") sert déjà de titre affiché ; rien
  n'empêche aujourd'hui d'y saisir un intitulé à pun.
- Cette feature ne modifie pas la structure des colonnes existante (intitulé, ordre), ni les
  mécanismes de post-its ou de vote (specs/001-retro-board-base) — extension additive uniquement.
