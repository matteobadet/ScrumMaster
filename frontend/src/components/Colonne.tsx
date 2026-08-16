import { useState, type FormEvent } from 'react';
import { PostIt } from './PostIt';
import type { ColonneState, PostItState } from '../types';

interface ColonneProps {
  colonne: ColonneState;
  postIts: PostItState[];
  currentParticipantId: string;
  onAddPostIt: (colonneId: string, texte: string) => void;
  onEditPostIt: (postItId: string, texte: string) => void;
  onDeletePostIt: (postItId: string) => void;
}

export function Colonne({
  colonne,
  postIts,
  currentParticipantId,
  onAddPostIt,
  onEditPostIt,
  onDeletePostIt,
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
            onEdit={(texte) => onEditPostIt(postIt.id, texte)}
            onDelete={() => onDeletePostIt(postIt.id)}
          />
        ))}
      </div>
      <form onSubmit={handleSubmit}>
        <textarea
          value={nouveauTexte}
          onChange={(e) => setNouveauTexte(e.target.value)}
          placeholder="Ajouter un post-it"
        />
        {error && <p role="alert">{error}</p>}
        <button type="submit">Ajouter</button>
      </form>
    </div>
  );
}
