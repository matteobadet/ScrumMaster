import type { ThemeSelection, ThemeSummary } from '../types';

interface ThemeEditorProps {
  themes: ThemeSummary[];
  value: ThemeSelection;
  onChange: (value: ThemeSelection) => void;
}

export function ThemeEditor({ themes, value, onChange }: ThemeEditorProps) {
  function switchToPredefined() {
    onChange({ kind: 'predefined', themeId: themes[0]?.id ?? '' });
  }

  function switchToCustom() {
    onChange({ kind: 'custom', nom: '', icone: '', contexte: '', colonnes: ['', ''] });
  }

  function updateColonne(index: number, intitule: string) {
    if (value.kind !== 'custom') {
      return;
    }
    const colonnes = value.colonnes.map((c, i) => (i === index ? intitule : c));
    onChange({ ...value, colonnes });
  }

  function addColonne() {
    if (value.kind !== 'custom') {
      return;
    }
    onChange({ ...value, colonnes: [...value.colonnes, ''] });
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
          {value.colonnes.map((colonne, index) => (
            <div key={index}>
              <input
                value={colonne}
                onChange={(e) => updateColonne(index, e.target.value)}
                placeholder={`Colonne ${index + 1}`}
              />
              {value.colonnes.length > 1 && (
                <button type="button" onClick={() => removeColonne(index)}>
                  Retirer
                </button>
              )}
            </div>
          ))}
          <button type="button" onClick={addColonne}>
            Ajouter une colonne
          </button>
        </div>
      )}
    </fieldset>
  );
}
