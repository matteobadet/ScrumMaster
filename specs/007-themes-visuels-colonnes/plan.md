# Implementation Plan: Thèmes Visuels par Colonne

**Branch**: `007-themes-visuels-colonnes` | **Date**: 2026-08-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/007-themes-visuels-colonnes/spec.md`

## Summary

Étendre l'entité `Colonne` déjà existante (specs/001-retro-board-base, portée par un `Thème` —
specs/004-themes-narratifs) avec deux attributs facultatifs propres à chaque colonne — une couleur
de fond, et une URL d'illustration (image hébergée en dehors de ScrumMaster, jamais récupérée ou
stockée côté serveur). Approche technique : extension additive du modèle de données, des DTOs de
transport déjà existants (REST + SignalR) et des composants frontend d'édition/affichage du thème ;
aucun nouvel endpoint, aucune nouvelle méthode de hub, aucune nouvelle dépendance ni infrastructure
de stockage de fichiers.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (backend) ; TypeScript 5 / React 19 (frontend) — inchangé par
rapport aux features précédentes

**Primary Dependencies**: Aucune nouvelle dépendance — réutilise ASP.NET Core Web API, SignalR, EF
Core + Npgsql, React déjà en place

**Storage**: PostgreSQL — extension de la table `Colonnes` existante (deux colonnes nullable),
même base `scrummaster`, migration EF Core additive ; aucun stockage de fichiers/blob/CDN (les
illustrations restent hébergées par des tiers, `research.md#2`)

**Testing**: xUnit + `Microsoft.AspNetCore.Mvc.Testing` (backend, intégration) — mêmes outils que
les features précédentes ; pas de suite de tests automatisés côté frontend à ce jour (aucune n'a
été mise en place dans le projet malgré la mention aspirationnelle de specs/004) — vérification
manuelle en navigateur, cohérent avec l'approche effectivement suivie depuis specs/001

**Target Platform**: Inchangé — conteneurs Linux sur le cluster k3s existant

**Project Type**: Application web (frontend React SPA + backend ASP.NET Core API/SignalR) —
extension de l'existant, pas de nouveau projet

**Performance Goals**: Association couleur + illustration à chaque colonne en moins de 2 minutes
(SC-001) ; propagation du changement de thème en cours de session dans le même délai que les
mutations déjà temps réel de specs/001-retro-board-base (quelques secondes, SC-002)

**Constraints**: Champs facultatifs uniquement — aucune migration de contenu requise pour les
colonnes et boards existants (FR-005, SC-003) ; aucune récupération ni stockage de l'image côté
serveur — chargement direct par le navigateur de chaque participant (FR-003, `research.md#2`) ;
URL d'illustration limitée aux adresses HTTPS syntaxiquement valides (FR-009, `research.md#3`)

**Scale/Scope**: Extension ciblée de 2 champs sur 1 entité déjà existante (`Colonne`), propagés à
travers les DTOs de thème/board déjà en place et 2 composants frontend (`ThemeEditor.tsx`,
`Colonne.tsx`) ; ajout d'un thème prédéfini entièrement habillé au catalogue de seed
(`research.md#5`) — aucun changement d'échelle par rapport aux features précédentes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Statut | Justification |
|----------|--------|----------------|
| I. Développement piloté par les spécifications | PASS | Ce plan fait suite à `spec.md` (validé, avec 2 clarifications résolues en session) ; aucune implémentation n'a démarré avant ce document. |
| II. Stack technique standardisée | PASS | ASP.NET Core + React + PostgreSQL, sans écart, aucune nouvelle dépendance. |
| III. MVP avant tout | PASS | Les 4 phases de la roadmap MVP sont déjà fonctionnelles ; cette feature enrichit la Phase 1 (thèmes de board, dans la continuité de specs/004) plutôt que d'anticiper une phase ultérieure — même positionnement que specs/004-themes-narratifs. |
| IV. Multi-tenant par conception | PASS | N'affecte pas le scoping par Area Path déjà en place ; `Colonne`/`Thème` ne sont pas des entités tenant-scopées et le restent ici. |
| V. Isolation du déploiement partagé | PASS | Migration EF Core additive sur la base `scrummaster` existante ; aucun manifeste k8s modifié ; le choix de l'URL externe plutôt que d'un upload de fichier (`research.md#2`) évite explicitement d'introduire une nouvelle dépendance d'infrastructure (stockage objet/CDN) qui aurait pu remettre en question cette isolation. |
| VI. Évolutivité sans sur-ingénierie | PASS | Couleur en texte libre plutôt qu'une palette imposée à maintenir (`research.md#1`, même logique que l'icône de thème, specs/004) ; illustration par URL externe plutôt qu'une bibliothèque d'images ou un système d'upload à construire (`research.md#2`) ; pas d'entité séparée pour l'habillage de colonne, deux colonnes nullable sur l'entité `Colonne` existante. |

Aucune violation à justifier — la section Complexity Tracking reste vide.

**Re-check post Phase 1 (design)** : `data-model.md` confirme que `Couleur`/`UrlIllustration` sont
deux colonnes nullable sur `Colonne`, sans nouvelle entité ni jointure (Principe VI) ;
`contracts/` étendent les DTOs et événements déjà existants sans nouvel endpoint ni méthode
(Principe II/III) ; aucun champ tenant-scopé n'est introduit (Principe IV, sans changement par
rapport aux features précédentes) ; aucune infrastructure de stockage de fichiers n'est ajoutée
(Principe V, `research.md#2`). Tous les principes restent PASS après conception détaillée.

## Project Structure

### Documentation (this feature)

```text
specs/007-themes-visuels-colonnes/
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
│   ├── Models/Colonne.cs                    # + Couleur, UrlIllustration (nullable)
│   ├── Dtos/ThemeDtos.cs                    # ThemeSummaryDto/ThemePersonnaliseDto : Colonnes en objets (intitulé + couleur? + urlIllustration?) au lieu de string[]
│   ├── Dtos/BoardDtos.cs                    # ColonneDto : + couleur?, urlIllustration?
│   ├── Services/EtapeService.cs             # ResolveThemeAsync/CopyTheme : propagation couleur/URL + validation HTTPS (FR-009) et longueurs
│   ├── Data/ThemeSeeder.cs                  # + un thème prédéfini entièrement habillé ("La rétro du randonneur", FR-008/US3) ; les 2 thèmes existants restent inchangés
│   └── Data/Migrations/                     # nouvelle migration additive (2 colonnes nullable sur Colonnes)
└── tests/ScrumMaster.Api.Tests/             # tests d'intégration existants étendus (création/changement de thème avec couleur/illustration) + nouveaux cas de rejet d'URL invalide

frontend/
└── src/
    ├── types.ts                             # ThemeSummary, ThemePersonnalise, ColonneState, ThemeSelection : colonnes en objets {intitule, couleur?, urlIllustration?}
    ├── components/ThemeEditor.tsx           # champs de saisie couleur + URL d'illustration par colonne (thème personnalisé)
    ├── components/Colonne.tsx               # rendu de la couleur de fond + de l'illustration de la colonne
    └── pages/BoardPage.tsx                  # inchangé en structure ; consomme les nouveaux champs via Colonne.tsx
```

**Structure Decision**: Extension pure de la structure "Application web" déjà en place — aucun
nouveau projet, aucun nouveau répertoire de premier niveau. Les modifications se limitent aux
fichiers listés ci-dessus.

## Complexity Tracking

> Aucune violation de la Constitution Check — section laissée vide intentionnellement.
