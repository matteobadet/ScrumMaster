import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useRealtimeBoard } from '../hooks/useRealtimeBoard';
import { participantStorage } from '../services/participantStorage';
import { boardsApi } from '../services/boardsApi';
import { Colonne } from '../components/Colonne';
import { ThemeEditor } from '../components/ThemeEditor';
import type { ThemeSelection, ThemeSummary } from '../types';

export function BoardPage() {
  const { boardId } = useParams<{ boardId: string }>();
  const participant = useMemo(() => (boardId ? participantStorage.load(boardId) : null), [boardId]);
  const { board, error, invoke } = useRealtimeBoard(boardId, participant);

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
        colonnes: colonnesNonVides,
      });
    } else {
      invoke('ChangeTheme', boardId, themeSelection.themeId, null);
    }

    setShowThemeEditor(false);
  }

  return (
    <div>
      <h1>
        {board.areaPath} — {board.iteration}
      </h1>
      <p>Participants : {board.participants.map((p) => p.nomAffiche).join(', ')}</p>
      <p>Mes votes restants : {board.mesVotesRestants ?? board.maxVotesParParticipant}</p>
      <p>
        Lien à partager pour rejoindre :{' '}
        <a href={`${window.location.origin}/join/${boardId}`}>{`${window.location.origin}/join/${boardId}`}</a>
      </p>

      {participant.role === 'Facilitateur' && (
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

      <div className="board">
        {board.colonnes.map((colonne) => (
          <Colonne
            key={colonne.id}
            colonne={colonne}
            colonnesDisponibles={board.colonnes}
            postIts={board.postIts.filter((p) => p.colonneId === colonne.id)}
            currentParticipantId={participant.participantId}
            votesRestants={board.mesVotesRestants}
            onAddPostIt={(colonneId, texte) => invoke('AddPostIt', boardId, colonneId, texte)}
            onEditPostIt={(postItId, texte) => invoke('EditPostIt', boardId, postItId, texte)}
            onDeletePostIt={(postItId) => invoke('DeletePostIt', boardId, postItId)}
            onMovePostIt={(postItId, colonneId) => invoke('MovePostIt', boardId, postItId, colonneId)}
            onVotePostIt={(postItId) => invoke('Vote', boardId, postItId)}
            onRemoveVotePostIt={(postItId) => invoke('RemoveVote', boardId, postItId)}
          />
        ))}
      </div>
    </div>
  );
}
