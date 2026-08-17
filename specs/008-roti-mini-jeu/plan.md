# Implementation Plan: Mini-jeu ROTI

**Branch**: `008-roti-mini-jeu` | **Date**: 2026-08-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/008-roti-mini-jeu/spec.md`

## Summary

Ajouter un second mini-jeu "ROTI" au catalogue existant (aux côtés de "Météo d'équipe",
specs/006-systeme-extensions-etapes), avec une échelle à 5 niveaux, un visuel par défaut rendu
côté client (emoji, comme "Météo d'équipe") et une personnalisation facultative du visuel par
niveau via URL d'image externe (même mécanisme que l'illustration de colonne,
specs/007-themes-visuels-colonnes). Réutilise intégralement le mécanisme de réponse à un mini-jeu
déjà existant (`RepondreMiniJeu`, upsert par `(EtapeId, ParticipantId)`) : aucun nouvel endpoint,
aucune nouvelle méthode de hub.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (backend) ; TypeScript 5 / React 19 (frontend) — inchangé

**Primary Dependencies**: Aucune nouvelle dépendance

**Storage**: PostgreSQL — deux nouvelles tables additives (`ReponsesRoti`, `VisuelsRoti`), une
ligne supplémentaire dans le catalogue `MiniJeuxCatalogue` (seed), migration EF Core additive

**Testing**: xUnit + `Microsoft.AspNetCore.Mvc.Testing` (backend, intégration) ; pas de suite
automatisée côté frontend (cohérent avec l'état actuel du projet) — vérification manuelle en
navigateur

**Target Platform**: Inchangé — conteneurs Linux sur le cluster k3s existant

**Project Type**: Application web (frontend React SPA + backend ASP.NET Core API/SignalR) —
extension de l'existant

**Performance Goals**: Réponse ROTI propagée en temps réel dans le même délai que les autres
interactions déjà en place (specs/001-retro-board-base) ; composition d'une étape ROTI personnalisée
en moins d'1 minute (SC-002)

**Constraints**: Le visuel par défaut de chaque niveau est rendu côté client (emoji), sans image
stockée ou récupérée côté serveur ; une personnalisation par URL suit exactement les mêmes règles
de validation qu'une illustration de colonne (HTTPS obligatoire, jamais récupérée côté serveur,
`research.md#2`)

**Scale/Scope**: Deux nouvelles entités liées à `Etape` (réponse + personnalisation visuelle par
niveau), extension du DTO union déjà existant (`EtapeDto`, `EtapeRequestDto`), un nouveau composant
frontend (`EtapeMiniJeuRoti.tsx`) et une adaptation de l'aiguillage par type de mini-jeu déjà
existant (`BoardPage.tsx`, `EtapeSequenceEditor.tsx`)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Statut | Justification |
|----------|--------|----------------|
| I. Développement piloté par les spécifications | PASS | Ce plan fait suite à `spec.md` (validé, aucune clarification nécessaire — choix explicitement délégué par l'utilisateur dans la demande initiale). |
| II. Stack technique standardisée | PASS | ASP.NET Core + React + PostgreSQL, sans écart, aucune nouvelle dépendance. |
| III. MVP avant tout | PASS | Les 4 phases de la roadmap MVP sont déjà fonctionnelles ; cette feature enrichit le catalogue de mini-jeux de la Phase 4 (specs/006) déjà livrée, sans anticiper de nouvelle phase. |
| IV. Multi-tenant par conception | PASS | N'affecte pas le scoping par Area Path déjà en place ; les nouvelles entités sont scopées par `Etape`/`Board` comme le reste. |
| V. Isolation du déploiement partagé | PASS | Migration EF Core additive sur la base `scrummaster` existante ; réutilise le mécanisme d'illustration par URL externe (aucun stockage de fichiers, `research.md#2`), cohérent avec specs/007. |
| VI. Évolutivité sans sur-ingénierie | PASS | Réutilise la même union étiquetée que specs/006 (un mini-jeu = son propre type de réponse, pas de mécanisme générique abstrait) ; visuel par défaut en emoji client, pas de bibliothèque d'images (`research.md#3`) ; personnalisation par niveau modélisée comme une petite entité clé-valeur sparse, même logique que `Colonne` (specs/007) plutôt qu'un blob JSON ou des colonnes fixes. |

Aucune violation à justifier — la section Complexity Tracking reste vide.

**Re-check post Phase 1 (design)** : `data-model.md` confirme deux entités additives
(`ReponseRoti`, `EtapeRotiVisuel`) sans jointure générique ni polymorphisme (Principe VI) ;
`contracts/` étendent les DTOs union déjà existants (`EtapeDto`, `EtapeRequestDto`) et le mécanisme
de hub déjà générique (`RepondreMiniJeu`, `ReponseMiniJeuChangee`) sans nouvel endpoint ni méthode
(Principe II/III). Tous les principes restent PASS après conception détaillée.

## Project Structure

### Documentation (this feature)

```text
specs/008-roti-mini-jeu/
├── plan.md                      # This file (/speckit-plan command output)
├── research.md                  # Phase 0 output (/speckit-plan command)
├── data-model.md                # Phase 1 output (/speckit-plan command)
├── quickstart.md                # Phase 1 output (/speckit-plan command)
├── contracts/                   # Phase 1 output (/speckit-plan command)
│   ├── rest-api-delta.md
│   └── realtime-hub-delta.md
└── tasks.md                     # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── src/ScrumMaster.Api/
│   ├── Models/
│   │   ├── ReponseRoti.cs                 # nouveau : EtapeId, ParticipantId, Niveau (enum NiveauRoti), DateReponse
│   │   ├── EtapeRotiVisuel.cs             # nouveau : EtapeId, Niveau, UrlIllustration (personnalisation facultative sparse)
│   │   └── Etape.cs                       # + List<ReponseRoti> ReponsesRoti, List<EtapeRotiVisuel> VisuelsRoti
│   ├── Dtos/EtapeDtos.cs                  # EtapeDto : + ReponsesRoti?, MonNiveauRoti?, VisuelsRoti? ; EtapeRequestDto : + RotiPersonnalisations? ; nouveau NiveauVisuelDto, ReponseRotiDto
│   ├── Services/MiniJeuService.cs         # RepondreAsync : aiguille sur MiniJeuCatalogue.TypeInterne (meteo-equipe vs roti) pour parser/upserter dans la bonne collection
│   ├── Services/EtapeService.cs           # CreerEtapeMiniJeuAsync : construit VisuelsRoti depuis RotiPersonnalisations (validation HTTPS/longueur, research.md#2)
│   ├── Data/ScrumMasterDbContext.cs       # + DbSet<ReponseRoti>, DbSet<EtapeRotiVisuel>, config (clés composites, contraintes de longueur)
│   ├── Data/MiniJeuSeeder.cs              # + entrée catalogue "ROTI" ; rendu idempotent par TypeInterne (même correctif que ThemeSeeder, specs/007)
│   └── Data/Migrations/                   # nouvelle migration additive (2 tables)
└── tests/ScrumMaster.Api.Tests/           # nouveau fichier RotiTests.cs (mirroring MiniJeuTests.cs)

frontend/
└── src/
    ├── types.ts                           # EtapeState : + reponsesRoti?, monNiveauRoti?, visuelsRoti? ; EtapeRequest : + rotiPersonnalisations?
    ├── components/EtapeMiniJeuRoti.tsx    # nouveau : échelle à 5 niveaux, emoji par défaut ou image personnalisée par niveau
    ├── components/EtapeSequenceEditor.tsx # composition d'une étape ROTI : personnalisation facultative par niveau (URL)
    └── pages/BoardPage.tsx                # renderEtape : aiguille sur etape.miniJeu?.typeInterne (meteo-equipe → EtapeMiniJeuMeteo, roti → EtapeMiniJeuRoti)
```

**Structure Decision**: Extension pure de la structure "Application web" déjà en place — aucun
nouveau projet, aucun nouveau répertoire de premier niveau.

## Complexity Tracking

> Aucune violation de la Constitution Check — section laissée vide intentionnellement.
