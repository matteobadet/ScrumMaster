import { useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import type { HubConnection } from '@microsoft/signalr';
import { createRetroBoardConnection } from '../services/realtimeClient';
import { boardsApi } from '../services/boardsApi';
import { participantStorage } from '../services/participantStorage';
import { Colonne } from '../components/Colonne';
import type { BoardState, PostItState } from '../types';

interface PostItAddedEvent {
  postIt: PostItState;
}

interface PostItUpdatedEvent {
  postItId: string;
  texte: string;
}

interface PostItDeletedEvent {
  postItId: string;
}

export function BoardPage() {
  const { boardId } = useParams<{ boardId: string }>();
  const [board, setBoard] = useState<BoardState | null>(null);
  const [error, setError] = useState<string | null>(null);
  const connectionRef = useRef<HubConnection | null>(null);

  const participant = useMemo(() => (boardId ? participantStorage.load(boardId) : null), [boardId]);

  useEffect(() => {
    if (!boardId || !participant) {
      return;
    }

    let cancelled = false;

    boardsApi
      .getBoard(boardId)
      .then((state) => {
        if (!cancelled) {
          setBoard(state);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setError('Impossible de charger le board.');
        }
      });

    const connection = createRetroBoardConnection();
    connectionRef.current = connection;

    connection.on('PostItAdded', ({ postIt }: PostItAddedEvent) => {
      setBoard((current) => (current ? { ...current, postIts: [...current.postIts, postIt] } : current));
    });

    connection.on('PostItUpdated', ({ postItId, texte }: PostItUpdatedEvent) => {
      setBoard((current) =>
        current
          ? {
              ...current,
              postIts: current.postIts.map((p) => (p.id === postItId ? { ...p, texte } : p)),
            }
          : current,
      );
    });

    connection.on('PostItDeleted', ({ postItId }: PostItDeletedEvent) => {
      setBoard((current) =>
        current ? { ...current, postIts: current.postIts.filter((p) => p.id !== postItId) } : current,
      );
    });

    connection
      .start()
      .then(() => connection.invoke('JoinBoard', boardId, participant.participantId))
      .catch(() => {
        if (!cancelled) {
          setError('Connexion temps réel indisponible.');
        }
      });

    return () => {
      cancelled = true;
      connection.stop();
    };
  }, [boardId, participant]);

  if (!boardId || !participant) {
    return <p>Vous devez d'abord créer ou rejoindre ce board.</p>;
  }

  if (error) {
    return <p role="alert">{error}</p>;
  }

  if (!board) {
    return <p>Chargement…</p>;
  }

  function invoke(method: string, ...args: unknown[]) {
    connectionRef.current?.invoke(method, ...args).catch((err: Error) => setError(err.message));
  }

  return (
    <div>
      <h1>
        {board.areaPath} — {board.iteration}
      </h1>
      <div className="board">
        {board.colonnes.map((colonne) => (
          <Colonne
            key={colonne.id}
            colonne={colonne}
            postIts={board.postIts.filter((p) => p.colonneId === colonne.id)}
            currentParticipantId={participant.participantId}
            onAddPostIt={(colonneId, texte) => invoke('AddPostIt', boardId, colonneId, texte)}
            onEditPostIt={(postItId, texte) => invoke('EditPostIt', boardId, postItId, texte)}
            onDeletePostIt={(postItId) => invoke('DeletePostIt', boardId, postItId)}
          />
        ))}
      </div>
    </div>
  );
}
