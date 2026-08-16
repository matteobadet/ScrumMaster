import { useEffect, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { boardsApi } from '../services/boardsApi';
import { participantStorage } from '../services/participantStorage';
import { ApiError } from '../services/apiClient';
import { ThemeEditor } from '../components/ThemeEditor';
import type { ThemeSelection, ThemeSummary } from '../types';

export function CreateBoardPage() {
  const navigate = useNavigate();
  const [themes, setThemes] = useState<ThemeSummary[]>([]);
  const [areaPath, setAreaPath] = useState('');
  const [iteration, setIteration] = useState('');
  const [themeSelection, setThemeSelection] = useState<ThemeSelection>({ kind: 'predefined', themeId: '' });
  const [nomAffiche, setNomAffiche] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    boardsApi
      .getThemes()
      .then((loaded) => {
        setThemes(loaded);
        if (loaded.length > 0) {
          setThemeSelection((current) =>
            current.kind === 'predefined' && !current.themeId
              ? { kind: 'predefined', themeId: loaded[0].id }
              : current,
          );
        }
      })
      .catch(() => setThemes([]));
  }, []);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);

    let colonnesNonVides: string[] = [];
    if (themeSelection.kind === 'custom') {
      colonnesNonVides = themeSelection.colonnes.map((c) => c.trim()).filter(Boolean);
      if (colonnesNonVides.length === 0) {
        setError('Un thème personnalisé doit comporter au moins une colonne.');
        return;
      }
    }

    setSubmitting(true);
    try {
      const response = await boardsApi.createBoard({
        areaPath,
        iteration,
        themeId: themeSelection.kind === 'predefined' ? themeSelection.themeId || null : null,
        themePersonnalise:
          themeSelection.kind === 'custom'
            ? { nom: themeSelection.nom || 'Thème personnalisé', colonnes: colonnesNonVides }
            : null,
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
        <ThemeEditor themes={themes} value={themeSelection} onChange={setThemeSelection} />
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
