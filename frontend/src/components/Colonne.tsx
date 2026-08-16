import { useState, type FormEvent } from 'react';
import { PostIt } from './PostIt';
import type { ColonneState, PostItState } from '../types';

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
    <div className="colonne">
      <h2>{colonne.intitule}</h2>
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
