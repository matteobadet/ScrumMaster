import type { ColonneSummaire, ThemeSelection, ThemeSummary } from '../types';

interface ThemeEditorProps {
  themes: ThemeSummary[];
  value: ThemeSelection;
  onChange: (value: ThemeSelection) => void;
}

function colonneVide(): ColonneSummaire {
  return { intitule: '', couleur: null, urlIllustration: null };
}

/** URL d'illustration valide si HTTPS (FR-009, cohérent avec la validation serveur — retour immédiat côté client). */
function urlIllustrationInvalide(url: string | null): boolean {
  if (!url) {
    return false;
  }
  try {
    return new URL(url).protocol !== 'https:';
  } catch {
    return true;
  }
}

/**
 * Nettoie la liste de colonnes d'un thème personnalisé avant envoi : filtre les intitulés vides,
 * rejette si une URL d'illustration non vide n'est pas en https:// (FR-009).
 */
export function buildColonnesPersonnalisees(colonnes: ColonneSummaire[]): { colonnes: ColonneSummaire[] } | { error: string } {
  const nonVides = colonnes.filter((c) => c.intitule.trim().length > 0).map((c) => ({ ...c, intitule: c.intitule.trim() }));

  if (nonVides.length === 0) {
    return { error: 'Un thème personnalisé doit comporter au moins une colonne.' };
  }

  if (nonVides.some((c) => urlIllustrationInvalide(c.urlIllustration))) {
    return { error: "L'illustration d'une colonne doit être une URL en https://." };
  }

  return { colonnes: nonVides };
}

export function ThemeEditor({ themes, value, onChange }: ThemeEditorProps) {
  function switchToPredefined() {
    onChange({ kind: 'predefined', themeId: themes[0]?.id ?? '' });
  }

  function switchToCustom() {
    onChange({ kind: 'custom', nom: '', icone: '', contexte: '', colonnes: [colonneVide(), colonneVide()] });
  }

  function updateColonne(index: number, patch: Partial<ColonneSummaire>) {
    if (value.kind !== 'custom') {
      return;
    }
    const colonnes = value.colonnes.map((c, i) => (i === index ? { ...c, ...patch } : c));
    onChange({ ...value, colonnes });
  }

  function addColonne() {
    if (value.kind !== 'custom') {
      return;
    }
    onChange({ ...value, colonnes: [...value.colonnes, colonneVide()] });
  }

  function removeColonne(index: number) {
    if (value.kind !== 'custom') {
      return;
    }
    onChange({ ...value, colonnes: value.colonnes.filter((_, i) => i !== index) });
  }

  return (
    <fieldset className="theme-editor">
      <legend>Thème</legend>
      <label>
        <input type="radio" checked={value.kind === 'predefined'} onChange={switchToPredefined} />
        Thème prédéfini
      </label>
      <label>
        <input type="radio" checked={value.kind === 'custom'} onChange={switchToCustom} />
        Thème personnalisé
      </label>

      {value.kind === 'predefined' ? (
        <select value={value.themeId} onChange={(e) => onChange({ kind: 'predefined', themeId: e.target.value })}>
          {themes.map((theme) => (
            <option key={theme.id} value={theme.id}>
              {theme.nom}
            </option>
          ))}
        </select>
      ) : (
        <div>
          <label>
            Nom du thème
            <input value={value.nom} onChange={(e) => onChange({ ...value, nom: e.target.value })} />
          </label>
          <label>
            Icône (facultatif)
            <input
              value={value.icone}
              onChange={(e) => onChange({ ...value, icone: e.target.value })}
              placeholder="🎅"
              maxLength={50}
            />
          </label>
          <label>
            Contexte (facultatif)
            <textarea
              value={value.contexte}
              onChange={(e) => onChange({ ...value, contexte: e.target.value })}
              placeholder="Plantez le décor de cette rétro…"
              maxLength={500}
            />
          </label>
          {value.colonnes.map((colonne, index) => {
            const urlInvalide = urlIllustrationInvalide(colonne.urlIllustration);
            return (
              <div key={index} className="colonne-editor-row">
                <input
                  value={colonne.intitule}
                  onChange={(e) => updateColonne(index, { intitule: e.target.value })}
                  placeholder={`Colonne ${index + 1}`}
                />
                <label>
                  Couleur (facultatif)
                  <input
                    type="color"
                    value={colonne.couleur ?? '#ffffff'}
                    onChange={(e) => updateColonne(index, { couleur: e.target.value })}
                  />
                </label>
                <label>
                  Illustration (facultatif)
                  <input
                    value={colonne.urlIllustration ?? ''}
                    onChange={(e) => updateColonne(index, { urlIllustration: e.target.value || null })}
                    placeholder="https://…"
                    maxLength={2048}
                  />
                </label>
                {urlInvalide && <p role="alert">L'illustration doit être une URL en https://</p>}
                {value.colonnes.length > 1 && (
                  <button type="button" onClick={() => removeColonne(index)}>
                    Retirer
                  </button>
                )}
              </div>
            );
          })}
          <button type="button" onClick={addColonne}>
            Ajouter une colonne
          </button>
        </div>
      )}
    </fieldset>
  );
}
