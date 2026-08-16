import { useState, type FormEvent } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { boardsApi } from '../services/boardsApi';
import { participantStorage } from '../services/participantStorage';
import { ApiError } from '../services/apiClient';

export function JoinBoardPage() {
  const { boardId } = useParams<{ boardId: string }>();
  const navigate = useNavigate();
  const [nomAffiche, setNomAffiche] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  if (!boardId) {
    return <p>Lien de board invalide.</p>;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);

    try {
      const response = await boardsApi.joinBoard(boardId!, nomAffiche);
      participantStorage.save(boardId!, {
        participantId: response.participantId,
        nomAffiche,
        role: response.role,
      });
      navigate(`/board/${boardId}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div>
      <h1>Rejoindre le board de rétrospective</h1>
      <form onSubmit={handleSubmit}>
        <label>
          Votre nom
          <input value={nomAffiche} onChange={(e) => setNomAffiche(e.target.value)} required />
        </label>
        {error && <p role="alert">{error}</p>}
        <button type="submit" disabled={submitting}>
          {submitting ? 'Connexion…' : 'Rejoindre'}
        </button>
      </form>
    </div>
  );
}
