import { useState } from 'react';
import { VoteCounter } from './VoteCounter';
import type { ColonneState, PostItState } from '../types';

interface PostItProps {
  postIt: PostItState;
  isAuthor: boolean;
  autresColonnes: ColonneState[];
  votesRestants: number | null;
  readOnly: boolean;
  onEdit: (texte: string) => void;
  onDelete: () => void;
  onMove: (colonneId: string) => void;
  onVote: () => void;
  onRemoveVote: () => void;
}

export function PostIt({
  postIt,
  isAuthor,
  autresColonnes,
  votesRestants,
  readOnly,
  onEdit,
  onDelete,
  onMove,
  onVote,
  onRemoveVote,
}: PostItProps) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(postIt.texte);
  const [error, setError] = useState<string | null>(null);

  function startEditing() {
    setDraft(postIt.texte);
    setError(null);
    setEditing(true);
  }

  function submitEdit() {
    if (!draft.trim()) {
      setError('Le texte ne peut pas être vide.');
      return;
    }
    onEdit(draft.trim());
    setEditing(false);
  }

  return (
    <div className="post-it">
      {editing ? (
        <>
          <textarea value={draft} onChange={(e) => setDraft(e.target.value)} />
          {error && <p role="alert">{error}</p>}
          <button type="button" onClick={submitEdit}>
            Enregistrer
          </button>
          <button type="button" onClick={() => setEditing(false)}>
            Annuler
          </button>
        </>
      ) : (
        <>
          <p>{postIt.texte}</p>
          <p className="post-it-meta">{postIt.auteur}</p>
          <VoteCounter
            nombreVotes={postIt.nombreVotes}
            aVote={postIt.voteDuParticipant}
            votesRestants={votesRestants}
            readOnly={readOnly}
            onVote={onVote}
            onRemoveVote={onRemoveVote}
          />
          {!readOnly && autresColonnes.length > 0 && (
            <label>
              Déplacer vers
              <select value="" onChange={(e) => e.target.value && onMove(e.target.value)}>
                <option value="" disabled>
                  Choisir une colonne
                </option>
                {autresColonnes.map((colonne) => (
                  <option key={colonne.id} value={colonne.id}>
                    {colonne.intitule}
                  </option>
                ))}
              </select>
            </label>
          )}
          {!readOnly && isAuthor && (
            <>
              <button type="button" onClick={startEditing}>
                Modifier
              </button>
              <button type="button" onClick={onDelete}>
                Supprimer
              </button>
            </>
          )}
        </>
      )}
    </div>
  );
}
