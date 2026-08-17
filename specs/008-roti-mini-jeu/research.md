# Phase 0 Research: Mini-jeu ROTI

## 1. Modélisation de la réponse ROTI

**Decision**: Nouvelle entité `ReponseRoti` (EtapeId, ParticipantId, Niveau, DateReponse), clé
composite `(EtapeId, ParticipantId)`, en tout point symétrique de `ReponseMeteoEquipe`
(specs/006-systeme-extensions-etapes). `Etape` gagne une collection dédiée `ReponsesRoti`, au même
niveau que `ReponsesMeteo`.

**Rationale**: Le mini-jeu "Météo d'équipe" n'utilise pas de mécanisme de réponse générique — sa
collection `ReponsesMeteo` est typée avec son propre enum `HumeurMeteo`. Le catalogue de mini-jeux
est un ensemble fixe et fermé (union étiquetée, research.md#1 de specs/006) : chaque mini-jeu
ajoute sa propre collection typée sur `Etape`, plutôt qu'une table de réponses générique
polymorphe. Introduire une table de réponses générique pour accueillir ROTI casserait cette
cohérence et ajouterait une abstraction (type de réponse variable) non demandée.

**Alternatives considered**:
- Table de réponses générique `ReponsesMiniJeu(EtapeId, ParticipantId, ValeurTexte)` réutilisable
  par tout futur mini-jeu : rejetée — perd le typage fort de la réponse (l'enum `NiveauRoti`
  garantit qu'une valeur invalide est rejetée à la compilation et par EF Core, pas seulement par
  validation applicative), et anticipe des mini-jeux futurs non spécifiés (Constitution Principe
  VI).

## 2. Visuel par défaut (sans configuration)

**Decision**: Le visuel par défaut de chaque niveau de l'échelle ROTI est un emoji fixe, défini
côté frontend (constante), exactement comme la "Météo d'équipe" (`EtapeMiniJeuMeteo.tsx`, tableau
`HUMEURS`). Aucune image, aucune URL, aucune donnée en base pour ce cas par défaut.

**Rationale**: FR-002 exige un visuel par défaut sans configuration. Le mini-jeu déjà existant
résout exactement ce besoin de la même façon (emoji client), sans jamais introduire d'image ni de
stockage. Réutiliser ce pattern évite d'introduire un mécanisme d'image pour le cas par défaut
alors qu'un simple emoji suffit (Constitution Principe VI).

**Alternatives considered**:
- URL d'image par défaut stockée en base pour chaque niveau (ex: `placehold.co`, comme le thème
  "La rétro du randonneur", specs/007-themes-visuels-colonnes#5) : rejetée pour le cas par défaut
  — un emoji rendu client est strictement plus simple et tout aussi démonstrable ; l'URL externe
  n'est nécessaire que lorsque le facilitateur choisit explicitement de personnaliser (US2).

## 3. Personnalisation du visuel par niveau (US2)

**Decision**: Nouvelle entité `EtapeRotiVisuel` (EtapeId, Niveau, UrlIllustration), clé composite
`(EtapeId, Niveau)`, une ligne uniquement pour les niveaux effectivement personnalisés (table
creuse — un niveau sans ligne correspondante affiche le visuel par défaut). Même règles de
validation que l'illustration de colonne (specs/007-themes-visuels-colonnes, research.md#3) : URL
HTTPS syntaxiquement valide, longueur ≤2048 caractères, jamais récupérée côté serveur.

**Rationale**: FR-005 à FR-008 reprennent explicitement le mécanisme déjà validé pour
l'illustration de colonne. Une entité dédiée, sparse, par `(Etape, Niveau)` suit le même principe
que `Colonne.UrlIllustration` (une ligne = un objet personnalisable), plutôt qu'un blob JSON sur
`Etape` (non interrogeable simplement, pas de contrainte de longueur par valeur) ou des colonnes
fixes `RotiUrlNiveau1..5` sur `Etape` (rigide, casserait si le nombre de niveaux changeait).

**Alternatives considered**:
- Colonnes fixes sur `Etape` (`RotiUrlNiveau1`...`RotiUrlNiveau5`) : rejetées — couplent le schéma
  au nombre exact de niveaux (5, `spec.md` Assumptions), rendant tout ajustement futur du barème
  plus coûteux qu'une ligne supplémentaire dans une table déjà générique par niveau.
- Réutiliser directement `Colonne` (déjà dotée de `Couleur`/`UrlIllustration`) en la rattachant à
  une étape ROTI plutôt qu'à un thème : rejetée — `Colonne` porte une sémantique et des relations
  propres aux étapes "Colonnes et post-its" (Theme, PostIts) ; la détourner pour un usage sans
  rapport romprait la cohérence du modèle (Constitution Principe VI, pas de réutilisation
  artificielle pour économiser une petite table).

## 4. Extension du DTO union et du mécanisme de réponse existant

**Decision**: `EtapeRequestDto` gagne un champ optionnel `RotiPersonnalisations`
(`IReadOnlyList<NiveauVisuelDto>?`), rempli uniquement quand le mini-jeu choisi est ROTI (validé
côté serveur : rejet explicite si des personnalisations sont fournies pour un mini-jeu qui n'est
pas ROTI). `EtapeDto` gagne `ReponsesRoti`, `MonNiveauRoti`, `VisuelsRoti` — mêmes noms/formes que
les champs Météo existants, adaptés au ROTI. Aucun nouvel endpoint ni méthode de hub : la méthode
déjà générique `RepondreMiniJeu(boardId, etapeId, reponse)` accepte la réponse ROTI comme chaîne de
caractères (nom du niveau), au même titre que la réponse Météo ; `MiniJeuService.RepondreAsync`
résout le `TypeInterne` du mini-jeu de l'étape pour savoir quel enum parser et quelle collection
mettre à jour. L'événement `ReponseMiniJeuChangee` déjà générique (`etapeId, participantId,
nomAffiche, reponse`) reste inchangé.

**Rationale**: Cohérent avec le mécanisme déjà en place (research.md#8 de specs/006 : REST pour
les lectures d'état complet, SignalR pour les nouvelles interactions) — ROTI n'introduit ni
nouvelle interaction ni nouvelle lecture, seulement un nouveau contenu transporté par les
mécanismes existants.

**Alternatives considered**:
- Nouvelle méthode de hub `RepondreRoti` dédiée : rejetée — dupliquerait `RepondreMiniJeu` sans
  bénéfice, alors qu'une simple résolution du type de mini-jeu côté service suffit.

## 5. Idempotence du seed de catalogue

**Decision**: `MiniJeuSeeder.EnsureSeededAsync` est rendu idempotent par mini-jeu (vérifie
l'existence par `TypeInterne` plutôt que globalement sur toute la table), même correctif que
`ThemeSeeder` (specs/007-themes-visuels-colonnes#T020) — nécessaire pour que "ROTI" apparaisse dans
une base déjà seedée avec "Météo d'équipe" sans réinitialisation.

**Rationale**: Le seeder actuel (`if (await db.MiniJeuxCatalogue.AnyAsync()) return;`) n'ajouterait
jamais ROTI à une base où "Météo d'équipe" existe déjà — bug déjà rencontré et corrigé pour
`ThemeSeeder` dans specs/007 ; même correctif appliqué ici par cohérence et pour éviter de le
redécouvrir en déploiement.
