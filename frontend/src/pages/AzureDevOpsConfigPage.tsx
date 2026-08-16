import { useState, type FormEvent } from 'react';
import { useParams } from 'react-router-dom';
import { boardsApi } from '../services/boardsApi';
import { ApiError } from '../services/apiClient';

export function AzureDevOpsConfigPage() {
  const { areaPath } = useParams<{ areaPath: string }>();
  const [organisation, setOrganisation] = useState('');
  const [projet, setProjet] = useState('');
  const [pat, setPat] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [succes, setSucces] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  if (!areaPath) {
    return <p>Équipe introuvable.</p>;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSucces(false);
    setSubmitting(true);
    try {
      await boardsApi.configurerAzureDevOps(areaPath!, { organisation, projet, pat });
      setPat('');
      setSucces(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Une erreur est survenue.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div>
      <h1>Accès Azure DevOps — {areaPath}</h1>
      <p>
        Configurez l'organisation, le projet et un Personal Access Token pour permettre à cette
        équipe de valider ses boards et d'importer/exporter des work items depuis Azure DevOps.
      </p>
      <form onSubmit={handleSubmit}>
        <label>
          Organisation
          <input value={organisation} onChange={(e) => setOrganisation(e.target.value)} required />
        </label>
        <label>
          Projet
          <input value={projet} onChange={(e) => setProjet(e.target.value)} required />
        </label>
        <label>
          Personal Access Token (PAT)
          <input type="password" value={pat} onChange={(e) => setPat(e.target.value)} required />
        </label>
        {error && <p role="alert">{error}</p>}
        {succes && <p role="status">Accès Azure DevOps configuré avec succès.</p>}
        <button type="submit" disabled={submitting}>
          {submitting ? 'Validation…' : 'Enregistrer'}
        </button>
      </form>
    </div>
  );
}
