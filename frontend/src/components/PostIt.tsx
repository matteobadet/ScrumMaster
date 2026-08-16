import { useState } from 'react';
import type { PostItState } from '../types';

interface PostItProps {
  postIt: PostItState;
  isAuthor: boolean;
  onEdit: (texte: string) => void;
  onDelete: () => void;
}

export function PostIt({ postIt, isAuthor, onEdit, onDelete }: PostItProps) {
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
          <p className="post-it-meta">
            {postIt.auteur} · {postIt.nombreVotes} vote(s)
          </p>
          {isAuthor && (
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
