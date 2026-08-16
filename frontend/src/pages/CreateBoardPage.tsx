import { useEffect, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { boardsApi } from '../services/boardsApi';
import { participantStorage } from '../services/participantStorage';
import { ApiError } from '../services/apiClient';
import type { ThemeSummary } from '../types';

export function CreateBoardPage() {
  const navigate = useNavigate();
  const [themes, setThemes] = useState<ThemeSummary[]>([]);
  const [areaPath, setAreaPath] = useState('');
  const [iteration, setIteration] = useState('');
  const [themeId, setThemeId] = useState<string>('');
  const [nomAffiche, setNomAffiche] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    boardsApi
      .getThemes()
      .then(setThemes)
      .catch(() => setThemes([]));
  }, []);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);

    try {
      const response = await boardsApi.createBoard({
        areaPath,
        iteration,
        themeId: themeId || null,
        themePersonnalise: null,
        maxVotesParParticipant: null,
        nomAffiche,
      });

      participantStorage.save(response.boardId, {
        participantId: response.participantId,
        nomAffiche,
        role: response.role,
      });

      navigate(`/board/${response.boardId}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div>
      <h1>Créer un board de rétrospective</h1>
      <form onSubmit={handleSubmit}>
        <label>
          Area Path
          <input value={areaPath} onChange={(e) => setAreaPath(e.target.value)} placeholder="ex : Krypton" required />
        </label>
        <label>
          Iteration / Sprint
          <input
            value={iteration}
            onChange={(e) => setIteration(e.target.value)}
            placeholder="ex : Sprint-138"
            required
          />
        </label>
        <label>
          Thème
          <select value={themeId} onChange={(e) => setThemeId(e.target.value)}>
            <option value="">Thème par défaut</option>
            {themes.map((theme) => (
              <option key={theme.id} value={theme.id}>
                {theme.nom}
              </option>
            ))}
          </select>
        </label>
        <label>
          Votre nom
          <input value={nomAffiche} onChange={(e) => setNomAffiche(e.target.value)} required />
        </label>
        {error && <p role="alert">{error}</p>}
        <button type="submit" disabled={submitting}>
          {submitting ? 'Création…' : 'Créer le board'}
        </button>
      </form>
    </div>
  );
}
