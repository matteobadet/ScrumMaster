# Implementation Plan: Nouveaux mini-jeux — Pendu et Lien externe

**Branch**: `011-pendu-lien-externe` | **Date**: 2026-08-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/011-pendu-lien-externe/spec.md`

## Summary

Ajouter deux entrées au catalogue de mini-jeux existant (specs/006, déjà étendu par ROTI en
specs/008) : "Pendu", un jeu partagé où l'équipe devine collectivement un mot lettre par lettre
(mécanisme propre, distinct de `RepondreMiniJeu`) ; et "Lien externe", une redirection vers un
outil de jeu tiers dont le facilitateur renseigne le nom et l'URL en direct une fois l'étape
active (même pattern que le changement de thème en direct).

## Technical Context

**Language/Version**: C# / .NET 8 (backend), TypeScript / React 19 (frontend) — inchangé.

**Primary Dependencies**: ASP.NET Core, EF Core 8, SignalR (backend, réutilisés) ; React + Vite
(frontend, réutilisés).

**Storage**: PostgreSQL — une nouvelle table (`LettresProposeesPendu`) et trois nouvelles colonnes
nullable sur `Etapes` (`MotAPendu`, `LienExterneNom`, `LienExterneUrl`), migration additive.

**Testing**: xUnit + `Microsoft.AspNetCore.Mvc.Testing` + `HubConnection` (SignalR), cohérent avec
`MiniJeuTests.cs`/`RotiTests.cs`/`BoardClosureTests.cs`.

**Target Platform**: identique au reste du projet (k3s, Traefik).

**Project Type**: web application (backend + frontend), extension de l'existant.

**Performance Goals**: une lettre proposée reflétée pour toute l'équipe en moins de 2 secondes
(SC-002) — diffusion SignalR immédiate, pas de polling, cohérent avec le reste du hub.

**Constraints**: le mot du Pendu ne doit jamais transiter en clair avant la fin de la partie
(research.md#2) ; le lien externe DOIT être HTTPS (FR-014) ; seul le facilitateur peut renseigner/
modifier le lien externe (FR-010).

**Scale/Scope**: une partie de Pendu = au plus quelques dizaines de lettres proposées (26 lettres
maximum possibles) — aucune pagination ni optimisation particulière nécessaire.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Statut | Justification |
|---|---|---|
| I. Développement piloté par les spécifications | PASS | spec.md validé avant ce plan ; la seule ambiguïté structurante (moment de saisie du lien externe) a été posée et résolue avec l'utilisateur avant de planifier. |
| II. Stack technique standardisée | PASS | ASP.NET Core (C#) backend, React frontend, PostgreSQL — aucun écart. |
| III. MVP avant tout | PASS | S'appuie sur la Phase 4 (extensions/mini-jeux, specs/006) déjà livrée ; n'anticipe aucune phase ultérieure. |
| IV. Multi-tenant par conception | PASS | Toute donnée nouvelle est rattachée à une `Etape` déjà scopée par `BoardId`/`AreaPath` — aucun changement au modèle de tenant. |
| V. Isolation du déploiement partagé | PASS | Aucun changement d'infrastructure/déploiement ; extension de code applicatif + une migration. |
| VI. Évolutivité sans sur-ingénierie | PASS | Le Pendu obtient sa propre méthode de hub uniquement parce que sa forme de donnée (journal partagé append-only) diffère structurellement de `RepondreMiniJeu`, pas par anticipation (research.md#1) ; le Lien externe réutilise tel quel le pattern de configuration en direct déjà existant plutôt que d'en inventer un nouveau (research.md#5). |

**Re-check post Phase 1 design**: PASS — le design (deux méthodes de hub ciblées, extraction d'un
helper de validation déjà dupliqué deux fois, une seule nouvelle table) ne fait apparaître aucune
violation nouvelle.

## Project Structure

### Documentation (this feature)

```text
specs/011-pendu-lien-externe/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── rest-api-delta.md
│   └── realtime-hub-delta.md
└── tasks.md              # Phase 2 output (/speckit-tasks command)
```

### Source Code (repository root)

```text
backend/
├── src/ScrumMaster.Api/
│   ├── Models/
│   │   ├── LettreProposeePendu.cs        # NOUVEAU
│   │   └── Etape.cs                       # MODIFIÉ : MotAPendu, LienExterneNom/Url, LettresProposeesPendu
│   ├── Data/
│   │   ├── ScrumMasterDbContext.cs        # MODIFIÉ : DbSet + configuration EF (clé composite)
│   │   ├── MiniJeuSeeder.cs               # MODIFIÉ : entrées "pendu", "lien-externe"
│   │   └── Migrations/                    # NOUVEAU : migration additive
│   ├── Dtos/
│   │   └── EtapeDtos.cs                   # MODIFIÉ : champs Pendu/Lien externe (EtapeRequestDto, EtapeDto)
│   ├── Services/
│   │   ├── UrlValidation.cs               # NOUVEAU : helper HTTPS partagé (research.md#6)
│   │   ├── EtapeService.cs                # MODIFIÉ : composition Pendu, réutilise UrlValidation
│   │   ├── BoardService.cs                # MODIFIÉ : BuildEtapeDto peuple les champs Pendu/Lien externe
│   │   └── MiniJeuService.cs              # MODIFIÉ : ProposerLettrePenduAsync, DefinirLienExterneAsync
│   └── Hubs/
│       └── RetroBoardHub.cs               # MODIFIÉ : ProposerLettrePendu, DefinirLienExterne
└── tests/ScrumMaster.Api.Tests/
    ├── PenduTests.cs                      # NOUVEAU
    └── LienExterneTests.cs                # NOUVEAU

frontend/
└── src/
    ├── types.ts                                    # MODIFIÉ : champs Pendu/Lien externe
    ├── hooks/useRealtimeBoard.ts                    # MODIFIÉ : LettrePenduProposee, LienExterneDefini
    ├── components/
    │   ├── EtapeMiniJeuPendu.tsx                   # NOUVEAU
    │   ├── EtapeMiniJeuLienExterne.tsx              # NOUVEAU
    │   └── EtapeSequenceEditor.tsx                  # MODIFIÉ : champ "mot" à la composition (Pendu uniquement)
    └── pages/BoardPage.tsx                          # MODIFIÉ : routage par typeInterne, invoke des 2 nouvelles méthodes
```

**Structure Decision**: Extension du backend/frontend existants (option "Web application" — déjà en
place). Une seule migration EF Core additive.

## Complexity Tracking

*Aucune violation de la Constitution — section non applicable.*
