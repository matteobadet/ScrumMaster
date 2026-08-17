import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useRealtimeBoard } from '../hooks/useRealtimeBoard';
import { participantStorage } from '../services/participantStorage';
import { boardsApi } from '../services/boardsApi';
import { Colonne } from '../components/Colonne';
import { ThemeEditor } from '../components/ThemeEditor';
import { EtapeMiniJeuMeteo } from '../components/EtapeMiniJeuMeteo';
import { EtapePollPersonnalise } from '../components/EtapePollPersonnalise';
import type { EtapeState, ThemeSelection, ThemeSummary } from '../types';

const TYPE_LABELS: Record<EtapeState['type'], string> = {
  ColonnesEtPostIts: 'Colonnes et post-its',
  MiniJeu: 'Mini-jeu',
  PollPersonnalise: 'Poll personnalisé',
};

export function BoardPage() {
  const { boardId } = useParams<{ boardId: string }>();
  const participant = useMemo(() => (boardId ? participantStorage.load(boardId) : null), [boardId]);
  const { board, error, invoke, repondrePollPersonnalise } = useRealtimeBoard(boardId, participant);

  const [themes, setThemes] = useState<ThemeSummary[]>([]);
  const [showThemeEditor, setShowThemeEditor] = useState(false);
  const [themeSelection, setThemeSelection] = useState<ThemeSelection>({ kind: 'predefined', themeId: '' });
  const [themeError, setThemeError] = useState<string | null>(null);

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

  if (!boardId) {
    return <p>Board introuvable.</p>;
  }

  if (!participant) {
    return (
      <p>
        Vous devez d'abord rejoindre ce board. <Link to={`/join/${boardId}`}>Rejoindre le board</Link>
      </p>
    );
  }

  if (error) {
    return <p role="alert">{error}</p>;
  }

  if (!board) {
    return <p>Chargement…</p>;
  }

  const estCloture = board.statut === 'Cloture';
  // Une seule étape à la fois est active (FR-004) ; les autres restent consultables en lecture
  // seule une fois terminées (FR-007), ou à venir (US1, specs/006-systeme-extensions-etapes).
  const etapeActive = board.etapes.find((e) => e.statut === 'Active');
  const derniereEtape = board.etapes[board.etapes.length - 1];
  const estDerniereEtapeActive = etapeActive !== undefined && etapeActive.id === derniereEtape.id;

  function avancerEtape() {
    const message = estDerniereEtapeActive
      ? 'Clôturer ce board ? Il passera en lecture seule pour tous les participants.'
      : "Passer à l'étape suivante ? L'étape actuelle deviendra consultable en lecture seule.";
    if (window.confirm(message)) {
      invoke('AvancerEtape', boardId);
    }
  }

  function applyThemeChange() {
    setThemeError(null);

    if (themeSelection.kind === 'custom') {
      const colonnesNonVides = themeSelection.colonnes.map((c) => c.trim()).filter(Boolean);
      if (colonnesNonVides.length === 0) {
        setThemeError('Un thème personnalisé doit comporter au moins une colonne.');
        return;
      }
      invoke('ChangeTheme', boardId, null, {
        nom: themeSelection.nom || 'Thème personnalisé',
        icone: themeSelection.icone.trim() || null,
        contexte: themeSelection.contexte.trim() || null,
        colonnes: colonnesNonVides,
      });
    } else {
      invoke('ChangeTheme', boardId, themeSelection.themeId, null);
    }

    setShowThemeEditor(false);
  }

  function renderColonnesEtPostIts(etape: EtapeState, readOnly: boolean) {
    return (
      <div className="board">
        {(etape.colonnes ?? []).map((colonne) => (
          <Colonne
            key={colonne.id}
            colonne={colonne}
            colonnesDisponibles={etape.colonnes ?? []}
            postIts={(etape.postIts ?? []).filter((p) => p.colonneId === colonne.id)}
            currentParticipantId={participant.participantId}
            votesRestants={etape.mesVotesRestants}
            readOnly={readOnly}
            onAddPostIt={(colonneId, texte) => invoke('AddPostIt', boardId, colonneId, texte)}
            onEditPostIt={(postItId, texte) => invoke('EditPostIt', boardId, postItId, texte)}
            onDeletePostIt={(postItId) => invoke('DeletePostIt', boardId, postItId)}
            onMovePostIt={(postItId, colonneId) => invoke('MovePostIt', boardId, postItId, colonneId)}
            onVotePostIt={(postItId) => invoke('Vote', boardId, postItId)}
            onRemoveVotePostIt={(postItId) => invoke('RemoveVote', boardId, postItId)}
            onExportPostIt={
              !readOnly && participant.role === 'Facilitateur' ? (postItId) => invoke('ExportPostIt', boardId, postItId) : undefined
            }
          />
        ))}
      </div>
    );
  }

  function renderEtape(etape: EtapeState, readOnly: boolean) {
    switch (etape.type) {
      case 'ColonnesEtPostIts':
        return renderColonnesEtPostIts(etape, readOnly);
      case 'MiniJeu':
        return (
          <EtapeMiniJeuMeteo etape={etape} readOnly={readOnly} onRepondre={(humeur) => invoke('RepondreMiniJeu', boardId, etape.id, humeur)} />
        );
      case 'PollPersonnalise':
        return (
          <EtapePollPersonnalise
            etape={etape}
            readOnly={readOnly}
            onRepondre={(optionId) => repondrePollPersonnalise(boardId, etape.id, optionId)}
          />
        );
    }
  }

  return (
    <div>
      <h1>
        {etapeActive?.theme?.icone && <span aria-hidden="true">{etapeActive.theme.icone} </span>}
        {board.areaPath} — {board.iteration}
      </h1>
      {etapeActive?.theme?.contexte && <p className="theme-contexte">{etapeActive.theme.contexte}</p>}
      <p>Participants : {board.participants.map((p) => p.nomAffiche).join(', ')}</p>
      {etapeActive?.type === 'ColonnesEtPostIts' && <p>Mes votes restants : {etapeActive.mesVotesRestants ?? board.maxVotesParParticipant}</p>}
      <p>
        Lien à partager pour rejoindre :{' '}
        <a href={`${window.location.origin}/join/${boardId}`}>{`${window.location.origin}/join/${boardId}`}</a>
      </p>

      {board.etapes.length > 1 && (
        <ol className="etape-sequence-apercu">
          {board.etapes.map((etape) => (
            <li key={etape.id} aria-current={etape.statut === 'Active' ? 'step' : undefined}>
              {TYPE_LABELS[etape.type]} — {etape.statut}
            </li>
          ))}
        </ol>
      )}

      {estCloture && <p role="status">Ce board est clôturé — lecture seule.</p>}

      {participant.role === 'Facilitateur' && !estCloture && (
        <>
          {etapeActive?.type === 'ColonnesEtPostIts' && (
            <div className="theme-change">
              <button type="button" onClick={() => setShowThemeEditor((v) => !v)}>
                {showThemeEditor ? 'Annuler' : 'Changer le thème'}
              </button>
              {showThemeEditor && (
                <>
                  <ThemeEditor themes={themes} value={themeSelection} onChange={setThemeSelection} />
                  {themeError && <p role="alert">{themeError}</p>}
                  <button type="button" onClick={applyThemeChange}>
                    Appliquer
                  </button>
                </>
              )}
            </div>
          )}
          <button type="button" onClick={avancerEtape}>
            {estDerniereEtapeActive ? 'Clôturer le board' : 'Étape suivante'}
          </button>
          {etapeActive?.type === 'ColonnesEtPostIts' && (
            <>
              <button type="button" onClick={() => invoke('ImportWorkItems', boardId)}>
                Importer les work items
              </button>
              <p>
                <Link to={`/equipe/${board.areaPath}/azure-devops`}>Configurer l'accès Azure DevOps de l'équipe</Link>
              </p>
            </>
          )}
        </>
      )}

      {etapeActive && renderEtape(etapeActive, false)}

      {board.etapes
        .filter((etape) => etape.statut === 'Terminee')
        .map((etape) => (
          <details key={etape.id} className="etape-terminee">
            <summary>{TYPE_LABELS[etape.type]} (terminée) — consultation en lecture seule</summary>
            {renderEtape(etape, true)}
          </details>
        ))}
    </div>
  );
}
