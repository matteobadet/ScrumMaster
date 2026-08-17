import { useEffect, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { boardsApi } from '../services/boardsApi';
import { participantStorage } from '../services/participantStorage';
import { ApiError } from '../services/apiClient';
import { ThemeEditor, buildColonnesPersonnalisees } from '../components/ThemeEditor';
import { EtapeSequenceEditor, buildEtapeRequests, creerEtapeBuilder, type EtapeBuilder } from '../components/EtapeSequenceEditor';
import type { ColonneSummaire, EquipeAzureDevOps, EtapeRequest, IterationAzureDevOps, MiniJeuRef, ThemeSelection, ThemeSummary } from '../types';

export function CreateBoardPage() {
  const navigate = useNavigate();
  const [themes, setThemes] = useState<ThemeSummary[]>([]);
  const [miniJeux, setMiniJeux] = useState<MiniJeuRef[]>([]);
  const [areaPath, setAreaPath] = useState('');
  const [iteration, setIteration] = useState('');
  const [themeSelection, setThemeSelection] = useState<ThemeSelection>({ kind: 'predefined', themeId: '' });
  const [sequenceMode, setSequenceMode] = useState(false);
  const [etapeBuilders, setEtapeBuilders] = useState<EtapeBuilder[]>([]);
  const [nomAffiche, setNomAffiche] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const [equipesConfigurees, setEquipesConfigurees] = useState<EquipeAzureDevOps[]>([]);
  const [areaPathMode, setAreaPathMode] = useState<'equipe' | 'libre'>('libre');
  const [iterations, setIterations] = useState<IterationAzureDevOps[]>([]);
  const [iterationsIndisponibles, setIterationsIndisponibles] = useState(false);

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

    boardsApi.getMiniJeux().then(setMiniJeux).catch(() => setMiniJeux([]));

    boardsApi
      .listerEquipesAvecAzureDevOps()
      .then((loaded) => {
        setEquipesConfigurees(loaded);
        if (loaded.length > 0) {
          setAreaPathMode('equipe');
          setAreaPath(loaded[0].areaPath);
        }
      })
      .catch(() => setEquipesConfigurees([]));
  }, []);

  // Sélection guidée de l'Iteration (US2, FR-005/FR-005a) : dès que l'Area Path choisi correspond
  // à une équipe configurée, on récupère ses Iterations réelles et on présélectionne le sprint en
  // cours. Repli silencieux en texte libre si Azure DevOps est injoignable (FR-007).
  useEffect(() => {
    if (areaPathMode !== 'equipe' || !areaPath) {
      setIterations([]);
      return;
    }

    setIterationsIndisponibles(false);
    boardsApi
      .obtenirIterationsAzureDevOps(areaPath)
      .then((loaded) => {
        setIterations(loaded);
        const enCours = loaded.find((i) => i.enCours);
        setIteration(enCours?.cheminIteration ?? loaded[0]?.cheminIteration ?? '');
      })
      .catch(() => {
        setIterations([]);
        setIterationsIndisponibles(true);
      });
  }, [areaPathMode, areaPath]);

  function selectionnerModeEquipe() {
    setAreaPathMode('equipe');
    setAreaPath(equipesConfigurees[0]?.areaPath ?? '');
  }

  function selectionnerModeLibre() {
    setAreaPathMode('libre');
    setAreaPath('');
    setIteration('');
    setIterations([]);
    setIterationsIndisponibles(false);
  }

  function activerSequenceMode() {
    setSequenceMode(true);
    setEtapeBuilders((current) => (current.length > 0 ? current : [creerEtapeBuilder('ColonnesEtPostIts', themes, miniJeux)]));
  }

  function activerEtapeUniqueMode() {
    setSequenceMode(false);
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);

    let colonnesNonVides: ColonneSummaire[] = [];
    if (!sequenceMode && themeSelection.kind === 'custom') {
      const built = buildColonnesPersonnalisees(themeSelection.colonnes);
      if (built.error) {
        setError(built.error);
        return;
      }
      colonnesNonVides = built.colonnes;
    }

    let etapes: EtapeRequest[] | undefined;
    if (sequenceMode) {
      const built = buildEtapeRequests(etapeBuilders);
      if (built.error) {
        setError(built.error);
        return;
      }
      etapes = built.requests;
    }

    setSubmitting(true);
    try {
      const response = await boardsApi.createBoard({
        areaPath,
        iteration,
        themeId: !sequenceMode && themeSelection.kind === 'predefined' ? themeSelection.themeId || null : null,
        themePersonnalise:
          !sequenceMode && themeSelection.kind === 'custom'
            ? {
                nom: themeSelection.nom || 'Thème personnalisé',
                icone: themeSelection.icone.trim() || null,
                contexte: themeSelection.contexte.trim() || null,
                colonnes: colonnesNonVides,
              }
            : null,
        maxVotesParParticipant: null,
        nomAffiche,
        etapes,
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
    <div className="page page-form">
      <h1>Créer un board de rétrospective</h1>
      <form className="card form" onSubmit={handleSubmit}>
        {equipesConfigurees.length > 0 && (
          <fieldset>
            <legend>Area Path</legend>
            <label>
              <input type="radio" checked={areaPathMode === 'equipe'} onChange={selectionnerModeEquipe} />
              Équipe déjà configurée avec Azure DevOps
            </label>
            <label>
              <input type="radio" checked={areaPathMode === 'libre'} onChange={selectionnerModeLibre} />
              Autre équipe (texte libre)
            </label>
          </fieldset>
        )}

        {areaPathMode === 'equipe' ? (
          <label>
            Area Path
            <select value={areaPath} onChange={(e) => setAreaPath(e.target.value)}>
              {equipesConfigurees.map((equipe) => (
                <option key={equipe.areaPath} value={equipe.areaPath}>
                  {equipe.areaPath}
                </option>
              ))}
            </select>
          </label>
        ) : (
          <label>
            Area Path
            <input value={areaPath} onChange={(e) => setAreaPath(e.target.value)} placeholder="ex : Krypton" required />
          </label>
        )}

        {areaPathMode === 'equipe' && !iterationsIndisponibles && iterations.length > 0 ? (
          <label>
            Iteration / Sprint
            <select value={iteration} onChange={(e) => setIteration(e.target.value)}>
              {iterations.map((it) => (
                <option key={it.cheminIteration} value={it.cheminIteration}>
                  {it.cheminIteration}
                  {it.enCours ? ' (en cours)' : ''}
                </option>
              ))}
            </select>
          </label>
        ) : (
          <label>
            Iteration / Sprint
            <input
              value={iteration}
              onChange={(e) => setIteration(e.target.value)}
              placeholder="ex : Sprint-138"
              required
            />
            {iterationsIndisponibles && (
              <p role="alert">
                Impossible de récupérer les Iterations depuis Azure DevOps pour le moment — saisissez-la manuellement.
              </p>
            )}
          </label>
        )}
        <fieldset>
          <legend>Composition du board</legend>
          <label>
            <input type="radio" checked={!sequenceMode} onChange={activerEtapeUniqueMode} />
            Étape unique (comportement actuel)
          </label>
          <label>
            <input type="radio" checked={sequenceMode} onChange={activerSequenceMode} />
            Séquence de plusieurs étapes
          </label>
        </fieldset>

        {sequenceMode ? (
          <EtapeSequenceEditor etapes={etapeBuilders} themes={themes} miniJeux={miniJeux} onChange={setEtapeBuilders} />
        ) : (
          <ThemeEditor themes={themes} value={themeSelection} onChange={setThemeSelection} />
        )}
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
