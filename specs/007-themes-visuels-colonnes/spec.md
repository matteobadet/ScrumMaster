# Feature Specification: Thèmes Visuels par Colonne

**Feature Branch**: `007-themes-visuels-colonnes`

**Created**: 2026-08-17

**Status**: Draft

**Input**: User description: "Habillage visuel des thèmes narratifs de rétrospective : au-delà du
nom/icône/contexte textuel déjà existant (specs/004-themes-narratifs), chaque colonne d'un thème
doit pouvoir porter sa propre identité visuelle — une couleur de fond dédiée et une
illustration/icône représentative — pour recréer l'effet 'univers immersif' illustré par la
capture d'écran fournie par l'utilisateur : un thème 'La rétro du randonneur' où chaque colonne
(La corde, Le rocher, La météo du voyage, Journal de randonnée, Trousse de secours) a sa propre
couleur pastel, sa propre illustration en haut à droite, un badge coloré pour son intitulé, et une
question directrice affichée sous le badge. Périmètre à couvrir : personnalisation visuelle par
colonne (couleur + illustration/icône) pour les thèmes prédéfinis fournis par l'équipe projet ET
pour les thèmes personnalisés créés par le facilitateur (specs/001-retro-board-base,
specs/004-themes-narratifs). Contexte projet : ScrumMaster est un outil de rétrospective
multi-équipes déjà doté d'un système de thèmes narratifs textuels (nom, icône unique, contexte,
liste de colonnes) ; cette feature enrichit ce système existant plutôt que de le remplacer."

## Clarifications

### Session 2026-08-17

- Q: Sous quelle forme l'icône/illustration de colonne est-elle fournie ? → A: Une image, plutôt
  qu'un emoji/texte libre — se rapproche du rendu de la capture d'écran de référence.
- Q: Pour cette image, le facilitateur colle-t-il une URL vers une image déjà hébergée ailleurs,
  ou téléverse-t-il un fichier stocké par ScrumMaster ? → A: Une URL externe collée par le
  facilitateur — aucun stockage de fichiers à construire côté ScrumMaster ; l'image est chargée
  directement par le navigateur de chaque participant depuis son hébergement d'origine.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Colorer chaque colonne d'un thème (Priority: P1)

Le facilitateur associe une couleur de fond distincte à chaque colonne d'un thème (prédéfini ou
personnalisé), pour que le board recrée visuellement l'univers du thème plutôt qu'une simple liste
de colonnes identiques.

**Why this priority**: C'est le changement qui a le plus d'impact visuel immédiat pour le moins
d'effort de saisie — une seule couleur par colonne suffit à transformer la perception du board, et
ne dépend d'aucune autre partie de la feature.

**Independent Test**: Peut être testé en créant un board avec un thème dont chaque colonne porte
une couleur de fond différente, puis en vérifiant que chaque colonne affiche bien sa couleur
propre sur le board.

**Acceptance Scenarios**:

1. **Given** un facilitateur qui crée un board ou change de thème, **When** il choisit un thème
   prédéfini dont les colonnes portent des couleurs, ou saisit une couleur pour chaque colonne de
   son thème personnalisé, **Then** chaque colonne du board affiche sa couleur de fond propre.
2. **Given** une colonne sans couleur associée, **When** un board utilise ce thème, **Then** cette
   colonne s'affiche avec l'apparence par défaut actuelle, sans espace vide ni erreur.

---

### User Story 2 - Illustrer chaque colonne d'une image (Priority: P2)

Le facilitateur associe une illustration représentative à chaque colonne d'un thème (prédéfini ou
personnalisé), en collant l'URL d'une image déjà hébergée ailleurs, affichée en évidence dans la
colonne, pour renforcer l'identité visuelle propre à chaque étape narrative du thème (ex : une
image de corde pour la colonne "La corde", un nuage pour "La météo du voyage").

**Why this priority**: Complète l'effet immersif recherché au-delà de la seule couleur (US1), mais
reste une amélioration indépendante — un thème peut être utile avec seulement des couleurs, ou
seulement des illustrations.

**Independent Test**: Peut être testé en créant un board avec un thème dont chaque colonne porte
une URL d'illustration, puis en vérifiant que chaque colonne affiche bien son image propre sur le
board.

**Acceptance Scenarios**:

1. **Given** un facilitateur qui crée un board ou change de thème, **When** il choisit un thème
   prédéfini dont les colonnes portent des illustrations, ou colle une URL d'image pour chaque
   colonne de son thème personnalisé, **Then** chaque colonne du board affiche son illustration
   propre, associée visuellement à cette colonne précise (pas seulement l'icône unique du thème
   entier).
2. **Given** une colonne sans illustration associée, **When** un board utilise ce thème, **Then**
   cette colonne s'affiche sans illustration, sans espace vide ni erreur.
3. **Given** une colonne dont l'URL d'illustration ne pointe plus vers une image accessible (lien
   cassé, image supprimée côté hébergeur externe), **When** un participant consulte le board,
   **Then** la colonne s'affiche sans l'illustration plutôt que de bloquer l'affichage du board ou
   de la colonne.

---

### User Story 3 - Démontrer l'effet avec un thème prédéfini entièrement habillé (Priority: P3)

Un facilitateur qui n'a pas envie de configurer manuellement les couleurs et illustrations de son
propre thème peut choisir un thème prédéfini du catalogue déjà entièrement habillé (couleur et
illustration sur chaque colonne), pour obtenir immédiatement l'effet "univers immersif" sans
configuration.

**Why this priority**: Rend la capacité démontrable et utilisable sans configuration manuelle, mais
n'est utile qu'une fois US1 et US2 en place (le mécanisme de personnalisation par colonne doit déjà
exister pour qu'un thème prédéfini puisse l'exploiter).

**Independent Test**: Peut être testé en choisissant, à la création d'un board, un thème prédéfini
annoncé comme entièrement habillé, sans effectuer aucune configuration manuelle, puis en vérifiant
que toutes ses colonnes affichent une couleur et une illustration dès la création du board.

**Acceptance Scenarios**:

1. **Given** le catalogue de thèmes prédéfinis, **When** le facilitateur choisit un thème
   entièrement habillé sans rien configurer, **Then** le board créé affiche immédiatement une
   couleur et une illustration propres à chacune de ses colonnes.

---

### Edge Cases

- Que se passe-t-il pour les thèmes et boards créés avant cette feature ? Leurs colonnes n'ont ni
  couleur ni illustration associée et continuent de s'afficher exactement comme aujourd'hui —
  aucune régression, aucune migration de contenu requise.
- Que se passe-t-il si le facilitateur choisit une couleur de fond qui rend le texte des post-its
  ou de l'intitulé de colonne difficile à lire ? Le système n'effectue aucune vérification
  automatique de contraste ; le choix reste à la discrétion du facilitateur (voir Assumptions).
- Que se passe-t-il si deux colonnes du même thème ont la même couleur, ou si une colonne a une
  couleur mais pas d'illustration (ou l'inverse) ? Autorisé dans tous les cas — couleur et
  illustration sont deux attributs indépendants, chacun facultatif individuellement.
- Que se passe-t-il quand le facilitateur change de thème en cours de session (mécanisme existant,
  specs/001-retro-board-base User Story 4) ? Les couleurs et illustrations de colonnes affichées
  passent immédiatement à celles du nouveau thème, comme le reste de l'habillage du thème
  (specs/004-themes-narratifs).
- Que se passe-t-il si le facilitateur saisit une URL qui n'est pas une adresse HTTPS valide ? Le
  système refuse l'enregistrement avec un message d'erreur explicite (voir FR-009).
- Que se passe-t-il si l'URL saisie est syntaxiquement valide mais ne pointe pas vers une image
  (page HTML, contenu inapproprié, etc.) ? Le système ne peut pas le détecter à la saisie (aucune
  récupération de l'image côté serveur, voir Assumptions) ; la colonne affichera alors ce que le
  navigateur du participant peut afficher depuis cette URL, ou rien si ce n'est pas une image.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT permettre d'associer une couleur de fond à chaque colonne d'un
  thème, qu'il s'agisse d'un thème prédéfini (catalogue) ou d'un thème personnalisé saisi par le
  facilitateur, indépendamment pour chaque colonne.
- **FR-002**: Le système DOIT afficher, pour chaque colonne du board, sa couleur de fond propre si
  elle est définie.
- **FR-003**: Le système DOIT permettre d'associer une illustration à chaque colonne d'un thème,
  qu'il s'agisse d'un thème prédéfini ou d'un thème personnalisé, sous la forme d'une URL fournie
  par le facilitateur vers une image déjà hébergée ailleurs (le facilitateur ne téléverse aucun
  fichier vers ScrumMaster), indépendamment pour chaque colonne.
- **FR-004**: Le système DOIT afficher, pour chaque colonne du board, l'image pointée par son URL
  d'illustration si elle est définie, de façon visuellement associée à cette colonne précise
  (distincte de l'icône unique du thème entier, specs/004-themes-narratifs).
- **FR-005**: La couleur de fond et l'illustration d'une colonne DOIVENT être facultatives chacune
  indépendamment — une colonne reste valide et utilisable sans elles, sans effet visuel indésirable
  (espace vide, placeholder) lorsqu'elles sont absentes.
- **FR-006**: Chaque choix de thème (à la création du board ou lors d'un changement en cours de
  session, specs/001-retro-board-base User Story 4) DOIT permettre de fournir la couleur et l'URL
  d'illustration de chaque colonne au même moment que son intitulé — cohérent avec le
  fonctionnement actuel où un nouveau choix de thème recrée entièrement ses colonnes plutôt que de
  modifier les existantes (specs/004-themes-narratifs, FR-006).
- **FR-007**: Lorsque le facilitateur change le thème appliqué à un board en cours de session, les
  couleurs et illustrations de colonnes affichées DOIVENT refléter immédiatement celles du nouveau
  thème.
- **FR-008**: Le catalogue de thèmes prédéfinis DOIT proposer au moins un thème dont toutes les
  colonnes portent déjà une couleur et une illustration, pour que la capacité soit démontrable sans
  configuration manuelle du facilitateur (cohérent avec l'approche déjà retenue pour le mini-jeu du
  catalogue, specs/006-systeme-extensions-etapes).
- **FR-009**: Le système DOIT refuser une URL d'illustration qui n'est pas une adresse HTTPS
  syntaxiquement valide, avec un message d'erreur explicite au moment de la saisie.
- **FR-010**: Le système NE DOIT PAS bloquer l'affichage d'une colonne, ni du reste du board, si
  son URL d'illustration devient inaccessible après coup (lien cassé) — la colonne s'affiche sans
  l'illustration.

### Key Entities *(include if feature involves data)*

- **Colonne** (extension de specs/001-retro-board-base, elle-même portée par un Thème —
  specs/004-themes-narratifs) : gagne deux attributs facultatifs propres à chaque colonne — une
  couleur de fond, et une URL d'illustration (image hébergée en dehors de ScrumMaster). Disponibles
  aussi bien pour les colonnes d'un thème prédéfini (catalogue) que pour celles d'un thème
  personnalisé saisi par le facilitateur. Distinct de l'icône et du contexte du Thème entier
  (specs/004-themes-narratifs), qui restent inchangés et continuent de s'appliquer au board dans
  son ensemble plutôt qu'à une colonne précise.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un facilitateur peut associer une couleur et une illustration à chaque colonne de son
  thème personnalisé en moins de 2 minutes.
- **SC-002**: 100% des participants rejoignant un board dont le thème a des colonnes habillées
  (couleur et/ou illustration) les voient sans action supplémentaire de leur part.
- **SC-003**: Les thèmes et boards existants avant cette feature restent utilisables sans
  régression visuelle après déploiement.
- **SC-004**: Le catalogue de thèmes prédéfinis propose au moins un thème entièrement habillé
  (couleur et illustration sur chaque colonne), utilisable et démontrable immédiatement sans
  configuration par le facilitateur.

## Assumptions

- La couleur de fond d'une colonne est saisie librement par le facilitateur (ex : sélecteur de
  couleur ou valeur hexadécimale), sans palette imposée ni gérée par le système — cohérent avec le
  principe de ne pas sur-concevoir (Constitution Principe VI).
- L'illustration d'une colonne est une image hébergée en dehors de ScrumMaster ; le système ne
  stocke, ne proxifie et ne récupère jamais lui-même le contenu de cette image côté serveur —
  chaque navigateur participant la charge directement depuis son URL d'origine, comme n'importe
  quelle image externe référencée sur une page web. Aucune infrastructure de stockage de fichiers
  n'est donc nécessaire pour cette feature.
- Cette feature se limite aux colonnes d'une étape de type "Colonnes et post-its"
  (specs/006-systeme-extensions-etapes) — elle ne couvre pas d'habillage visuel pour les étapes de
  type Mini-jeu ou Poll personnalisé.
- Aucune vérification automatique de contraste ou d'accessibilité des *couleurs* choisies n'est
  effectuée par le système à la saisie ; le choix d'une combinaison lisible reste à la discrétion
  du facilitateur (le texte affiché par-dessus une couleur de colonne est en revanche
  automatiquement rendu lisible côté client — voir Amendement ci-dessous).
- Les thèmes et boards créés avant cette feature restent utilisables sans migration de contenu :
  leurs colonnes, sans couleur ni illustration associée, conservent leur apparence actuelle.

## Amendement (2026-08-17, retour utilisateur post-implémentation)

- **Sous-titre de colonne** : l'Assumption initiale excluant la question directrice par colonne
  est revue — un attribut facultatif `SousTitre` (≤150 caractères) est ajouté à `Colonne`, affiché
  sous l'intitulé et l'illustration. Disponible pour les thèmes prédéfinis et personnalisés, selon
  le même mécanisme que `Couleur`/`UrlIllustration`. Le thème prédéfini "La rétro du randonneur"
  reprend les questions directrices de la capture d'écran de référence originale. Le bloc
  "Contexte" du thème entier (specs/004-themes-narratifs) continue par ailleurs de porter le
  narratif au niveau du board dans son ensemble.
- **Lisibilité automatique du texte de colonne** : le titre et le sous-titre d'une colonne
  calculent désormais une couleur de texte contrastante à partir de `Couleur` (luminance perçue),
  plutôt que d'utiliser la couleur de texte fixe du thème de l'app — corrige un défaut découvert en
  test manuel où certaines couleurs de colonne rendaient le titre illisible. N'est pas une
  vérification/rejet à la saisie (l'Assumption ci-dessus reste vraie pour la couleur elle-même) :
  c'est un ajustement purement visuel au rendu.
- **Lisibilité des listes déroulantes natives** : correction d'un défaut de contraste des options
  de `<select>` (ex : "Déplacer vers"), sans lien avec l'habillage de colonne mais découvert dans le
  même passage de test manuel.
