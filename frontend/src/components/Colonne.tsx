import { useState, type FormEvent } from 'react';
import { PostIt } from './PostIt';
import type { ColonneState, PostItState } from '../types';

/**
 * Couleur de texte lisible par-dessus une couleur de fond hexadécimale arbitraire choisie par le
 * facilitateur (luminance perçue, formule WCAG simplifiée). La saisie couleur (`ThemeEditor.tsx`)
 * ne produit que du `#rrggbb` ; tout autre format retombe sur la couleur de texte par défaut du
 * thème de l'app plutôt que de risquer un mauvais calcul.
 */
function texteLisibleSur(couleurFond: string | null): string | undefined {
  const match = couleurFond ? /^#([0-9a-f]{6})$/i.exec(couleurFond.trim()) : null;
  if (!match) {
    return undefined;
  }

  const hex = match[1];
  const r = parseInt(hex.slice(0, 2), 16);
  const g = parseInt(hex.slice(2, 4), 16);
  const b = parseInt(hex.slice(4, 6), 16);
  const luminancePercue = (0.299 * r + 0.587 * g + 0.114 * b) / 255;

  return luminancePercue > 0.6 ? '#1a1625' : '#f4f2f9';
}

interface ColonneProps {
  colonne: ColonneState;
  colonnesDisponibles: ColonneState[];
  postIts: PostItState[];
  currentParticipantId: string;
  votesRestants: number | null;
  readOnly: boolean;
  onAddPostIt: (colonneId: string, texte: string) => void;
  onEditPostIt: (postItId: string, texte: string) => void;
  onDeletePostIt: (postItId: string) => void;
  onMovePostIt: (postItId: string, colonneId: string) => void;
  onVotePostIt: (postItId: string) => void;
  onRemoveVotePostIt: (postItId: string) => void;
  onExportPostIt?: (postItId: string) => void;
}

export function Colonne({
  colonne,
  colonnesDisponibles,
  postIts,
  currentParticipantId,
  votesRestants,
  readOnly,
  onAddPostIt,
  onEditPostIt,
  onDeletePostIt,
  onMovePostIt,
  onVotePostIt,
  onRemoveVotePostIt,
  onExportPostIt,
}: ColonneProps) {
  const [nouveauTexte, setNouveauTexte] = useState('');
  const [error, setError] = useState<string | null>(null);

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!nouveauTexte.trim()) {
      setError('Le texte ne peut pas être vide.');
      return;
    }
    setError(null);
    onAddPostIt(colonne.id, nouveauTexte.trim());
    setNouveauTexte('');
  }

  return (
    <div className="colonne" style={colonne.couleur ? { backgroundColor: colonne.couleur } : undefined}>
      <div className="colonne-header" style={{ color: texteLisibleSur(colonne.couleur) }}>
        <div className="colonne-header-top">
          <h2>{colonne.intitule}</h2>
          {colonne.urlIllustration && (
            <img
              className="colonne-illustration"
              src={colonne.urlIllustration}
              alt=""
              aria-hidden="true"
              // Repli silencieux si le lien est cassé (FR-010) — la colonne reste utilisable.
              onError={(e) => {
                e.currentTarget.style.display = 'none';
              }}
            />
          )}
        </div>
        {colonne.sousTitre && <p className="colonne-sous-titre">{colonne.sousTitre}</p>}
      </div>
      <div className="post-its">
        {postIts.map((postIt) => (
          <PostIt
            key={postIt.id}
            postIt={postIt}
            isAuthor={postIt.auteurParticipantId === currentParticipantId}
            autresColonnes={colonnesDisponibles.filter((c) => c.id !== colonne.id)}
            votesRestants={votesRestants}
            readOnly={readOnly}
            onEdit={(texte) => onEditPostIt(postIt.id, texte)}
            onDelete={() => onDeletePostIt(postIt.id)}
            onMove={(colonneId) => onMovePostIt(postIt.id, colonneId)}
            onVote={() => onVotePostIt(postIt.id)}
            onRemoveVote={() => onRemoveVotePostIt(postIt.id)}
            onExport={onExportPostIt ? () => onExportPostIt(postIt.id) : undefined}
          />
        ))}
      </div>
      {!readOnly && (
        <form onSubmit={handleSubmit}>
          <textarea
            value={nouveauTexte}
            onChange={(e) => setNouveauTexte(e.target.value)}
            placeholder="Ajouter un post-it"
          />
          {error && <p role="alert">{error}</p>}
          <button type="submit">Ajouter</button>
        </form>
      )}
    </div>
  );
}
