import { useState } from 'react';
import type { EtapeState } from '../types';
import { urlIllustrationInvalide } from './ThemeEditor';

interface EtapeMiniJeuLienExterneProps {
  etape: EtapeState;
  readOnly: boolean;
  estFacilitateur: boolean;
  onDefinirLien: (nom: string, url: string) => void;
}

/**
 * Étape "Lien externe" — le facilitateur saisit/modifie le lien en direct pendant que l'étape est
 * active (US2, specs/011-pendu-lien-externe) ; aucun contenu à la composition.
 */
export function EtapeMiniJeuLienExterne({ etape, readOnly, estFacilitateur, onDefinirLien }: EtapeMiniJeuLienExterneProps) {
  const [nom, setNom] = useState('');
  const [url, setUrl] = useState('');
  const [editing, setEditing] = useState(false);

  const lienDefini = Boolean(etape.lienExterneUrl);
  const urlInvalide = urlIllustrationInvalide(url || null);

  function soumettre() {
    if (!nom.trim() || !url.trim() || urlInvalide) {
      return;
    }
    onDefinirLien(nom.trim(), url.trim());
    setEditing(false);
  }

  return (
    <div className="etape-mini-jeu">
      <h2>{etape.miniJeu?.nom ?? 'Mini-jeu'}</h2>

      {lienDefini && !editing && (
        <p>
          <a className="btn-primary" href={etape.lienExterneUrl!} target="_blank" rel="noopener noreferrer">
            Rejoindre {etape.lienExterneNom}
          </a>
        </p>
      )}

      {!lienDefini && !editing && <p>En attente du facilitateur — aucun lien n'a encore été renseigné.</p>}

      {!readOnly && estFacilitateur && !editing && (
        <button type="button" onClick={() => setEditing(true)}>
          {lienDefini ? 'Modifier le lien' : 'Renseigner le lien'}
        </button>
      )}

      {!readOnly && estFacilitateur && editing && (
        <div className="lien-externe-form">
          <label>
            Nom du jeu
            <input value={nom} onChange={(e) => setNom(e.target.value)} placeholder="Gartic Phone" />
          </label>
          <label>
            URL
            <input value={url} onChange={(e) => setUrl(e.target.value)} placeholder="https://…" />
          </label>
          {urlInvalide && <p role="alert">L'URL doit être une adresse en https://</p>}
          <div className="lien-externe-form-actions">
            <button type="button" onClick={soumettre}>
              Valider
            </button>
            <button type="button" onClick={() => setEditing(false)}>
              Annuler
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
