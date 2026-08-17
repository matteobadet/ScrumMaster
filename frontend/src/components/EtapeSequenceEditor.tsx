import type { EtapeRequest, MiniJeuRef, ThemeSelection, ThemeSummary } from '../types';
import { ThemeEditor, buildColonnesPersonnalisees, urlIllustrationInvalide } from './ThemeEditor';

/** Niveaux de l'échelle ROTI (specs/008-roti-mini-jeu, data-model.md), pour la personnalisation par niveau. */
const NIVEAUX_ROTI: { valeur: string; libelle: string }[] = [
  { valeur: 'PerteDeTemps', libelle: 'Perte de temps' },
  { valeur: 'PeuRentable', libelle: 'Peu rentable' },
  { valeur: 'MoyennementRentable', libelle: 'Moyennement rentable' },
  { valeur: 'Rentable', libelle: 'Rentable' },
  { valeur: 'TresRentable', libelle: 'Très rentable' },
];

export interface EtapeBuilder {
  id: string;
  type: 'ColonnesEtPostIts' | 'MiniJeu' | 'PollPersonnalise';
  themeSelection: ThemeSelection;
  miniJeuCatalogueId: string;
  question: string;
  options: string[];
  /** URL d'illustration personnalisée par niveau ROTI (specs/008-roti-mini-jeu, US2) ; vide si non personnalisé. */
  rotiVisuels: Record<string, string>;
}

function genererId(): string {
  return typeof crypto !== 'undefined' && 'randomUUID' in crypto ? crypto.randomUUID() : Math.random().toString(36).slice(2);
}

export function creerEtapeBuilder(
  type: EtapeBuilder['type'],
  themes: ThemeSummary[],
  miniJeux: MiniJeuRef[],
  id: string = genererId(),
): EtapeBuilder {
  return {
    id,
    type,
    themeSelection: { kind: 'predefined', themeId: themes[0]?.id ?? '' },
    miniJeuCatalogueId: miniJeux[0]?.id ?? '',
    question: '',
    options: ['', ''],
    rotiVisuels: {},
  };
}

/** Convertit la séquence composée en requête API, ou retourne une erreur de validation côté client. */
export function buildEtapeRequests(
  etapes: EtapeBuilder[],
  miniJeux: MiniJeuRef[],
): { requests: EtapeRequest[]; error?: undefined } | { error: string } {
  const requests: EtapeRequest[] = [];

  for (const etape of etapes) {
    if (etape.type === 'ColonnesEtPostIts') {
      if (etape.themeSelection.kind === 'custom') {
        const built = buildColonnesPersonnalisees(etape.themeSelection.colonnes);
        if (built.error) {
          return { error: built.error };
        }
        requests.push({
          type: 'ColonnesEtPostIts',
          themePersonnalise: {
            nom: etape.themeSelection.nom || 'Thème personnalisé',
            icone: etape.themeSelection.icone.trim() || null,
            contexte: etape.themeSelection.contexte.trim() || null,
            colonnes: built.colonnes,
          },
        });
      } else {
        requests.push({ type: 'ColonnesEtPostIts', themeId: etape.themeSelection.themeId || null });
      }
    } else if (etape.type === 'MiniJeu') {
      if (!etape.miniJeuCatalogueId) {
        return { error: 'Un mini-jeu doit être choisi pour chaque étape de type Mini-jeu.' };
      }

      const estRoti = miniJeux.find((m) => m.id === etape.miniJeuCatalogueId)?.typeInterne === 'roti';
      const rotiPersonnalisations = estRoti
        ? Object.entries(etape.rotiVisuels)
            .filter(([, url]) => url.trim().length > 0)
            .map(([niveau, url]) => ({ niveau, urlIllustration: url.trim() }))
        : [];

      if (rotiPersonnalisations.some((v) => urlIllustrationInvalide(v.urlIllustration))) {
        return { error: "L'illustration d'un niveau ROTI doit être une URL en https://." };
      }

      requests.push({
        type: 'MiniJeu',
        miniJeuCatalogueId: etape.miniJeuCatalogueId,
        rotiPersonnalisations: rotiPersonnalisations.length > 0 ? rotiPersonnalisations : null,
      });
    } else {
      const question = etape.question.trim();
      if (!question) {
        return { error: 'Une question est requise pour chaque étape de type Poll personnalisé.' };
      }
      const optionsNonVides = etape.options.map((o) => o.trim()).filter(Boolean);
      if (optionsNonVides.length < 2) {
        return { error: 'Une étape de poll personnalisé doit avoir au moins deux options de réponse.' };
      }
      requests.push({ type: 'PollPersonnalise', question, options: optionsNonVides });
    }
  }

  return { requests };
}

interface EtapeSequenceEditorProps {
  etapes: EtapeBuilder[];
  themes: ThemeSummary[];
  miniJeux: MiniJeuRef[];
  onChange: (etapes: EtapeBuilder[]) => void;
}

/** Composition explicite d'une séquence de plusieurs étapes (US1, specs/006-systeme-extensions-etapes). */
export function EtapeSequenceEditor({ etapes, themes, miniJeux, onChange }: EtapeSequenceEditorProps) {
  function update(index: number, patch: Partial<EtapeBuilder>) {
    onChange(etapes.map((e, i) => (i === index ? { ...e, ...patch } : e)));
  }

  function remove(index: number) {
    onChange(etapes.filter((_, i) => i !== index));
  }

  function add(type: EtapeBuilder['type']) {
    onChange([...etapes, creerEtapeBuilder(type, themes, miniJeux)]);
  }

  function move(index: number, direction: -1 | 1) {
    const target = index + direction;
    if (target < 0 || target >= etapes.length) {
      return;
    }
    const next = [...etapes];
    [next[index], next[target]] = [next[target], next[index]];
    onChange(next);
  }

  function updateOption(etapeIndex: number, optionIndex: number, value: string) {
    const options = etapes[etapeIndex].options.map((o, i) => (i === optionIndex ? value : o));
    update(etapeIndex, { options });
  }

  function addOption(etapeIndex: number) {
    update(etapeIndex, { options: [...etapes[etapeIndex].options, ''] });
  }

  function removeOption(etapeIndex: number, optionIndex: number) {
    update(etapeIndex, { options: etapes[etapeIndex].options.filter((_, i) => i !== optionIndex) });
  }

  function updateRotiVisuel(etapeIndex: number, niveau: string, url: string) {
    update(etapeIndex, { rotiVisuels: { ...etapes[etapeIndex].rotiVisuels, [niveau]: url } });
  }

  return (
    <fieldset className="etape-sequence-editor">
      <legend>Séquence d'étapes</legend>
      {etapes.map((etape, index) => (
        <div key={etape.id} className="etape-builder">
          <p>
            Étape {index + 1}
            <label>
              Type
              <select
                value={etape.type}
                onChange={(e) => {
                  const type = e.target.value as EtapeBuilder['type'];
                  update(index, creerEtapeBuilder(type, themes, miniJeux, etape.id));
                }}
              >
                <option value="ColonnesEtPostIts">Colonnes et post-its</option>
                <option value="MiniJeu">Mini-jeu</option>
                <option value="PollPersonnalise">Poll personnalisé</option>
              </select>
            </label>
          </p>

          {etape.type === 'ColonnesEtPostIts' && (
            <ThemeEditor themes={themes} value={etape.themeSelection} onChange={(themeSelection) => update(index, { themeSelection })} />
          )}

          {etape.type === 'MiniJeu' && (
            <label>
              Mini-jeu
              <select value={etape.miniJeuCatalogueId} onChange={(e) => update(index, { miniJeuCatalogueId: e.target.value })}>
                {miniJeux.map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.nom}
                  </option>
                ))}
              </select>
              {miniJeux.length === 0 && <p role="alert">Aucun mini-jeu disponible dans le catalogue.</p>}
            </label>
          )}

          {etape.type === 'MiniJeu' && miniJeux.find((m) => m.id === etape.miniJeuCatalogueId)?.typeInterne === 'roti' && (
            <div className="roti-personnalisation">
              <p>Visuel personnalisé par niveau (facultatif — sinon emoji par défaut)</p>
              {NIVEAUX_ROTI.map((niveau) => {
                const url = etape.rotiVisuels[niveau.valeur] ?? '';
                const urlInvalide = urlIllustrationInvalide(url || null);
                return (
                  <label key={niveau.valeur}>
                    {niveau.libelle}
                    <input
                      value={url}
                      onChange={(e) => updateRotiVisuel(index, niveau.valeur, e.target.value)}
                      placeholder="https://…"
                    />
                    {urlInvalide && <p role="alert">L'illustration doit être une URL en https://</p>}
                  </label>
                );
              })}
            </div>
          )}

          {etape.type === 'PollPersonnalise' && (
            <div>
              <label>
                Question
                <input
                  value={etape.question}
                  onChange={(e) => update(index, { question: e.target.value })}
                  placeholder="On garde la mêlée ?"
                />
              </label>
              {etape.options.map((option, optionIndex) => (
                <div key={optionIndex}>
                  <input
                    value={option}
                    onChange={(e) => updateOption(index, optionIndex, e.target.value)}
                    placeholder={`Option ${optionIndex + 1}`}
                  />
                  {etape.options.length > 2 && (
                    <button type="button" onClick={() => removeOption(index, optionIndex)}>
                      Retirer
                    </button>
                  )}
                </div>
              ))}
              <button type="button" onClick={() => addOption(index)}>
                Ajouter une option
              </button>
            </div>
          )}

          <div className="etape-builder-actions">
            <button type="button" onClick={() => move(index, -1)} disabled={index === 0}>
              Monter
            </button>
            <button type="button" onClick={() => move(index, 1)} disabled={index === etapes.length - 1}>
              Descendre
            </button>
            <button type="button" onClick={() => remove(index)} disabled={etapes.length <= 1}>
              Retirer cette étape
            </button>
          </div>
          <hr />
        </div>
      ))}

      <div className="etape-sequence-add">
        <button type="button" onClick={() => add('ColonnesEtPostIts')}>
          + Colonnes et post-its
        </button>
        <button type="button" onClick={() => add('MiniJeu')}>
          + Mini-jeu
        </button>
        <button type="button" onClick={() => add('PollPersonnalise')}>
          + Poll personnalisé
        </button>
      </div>
    </fieldset>
  );
}
