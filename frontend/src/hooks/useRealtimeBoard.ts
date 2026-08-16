import { useEffect, useRef, useState } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { createRetroBoardConnection } from '../services/realtimeClient';
import { boardsApi } from '../services/boardsApi';
import type { BoardState, CurrentParticipant, ParticipantState, PostItState } from '../types';

interface PostItAddedEvent {
  postIt: PostItState;
}

interface PostItUpdatedEvent {
  postItId: string;
  texte: string;
}

interface PostItMovedEvent {
  postItId: string;
  colonneId: string;
}

interface PostItDeletedEvent {
  postItId: string;
}

interface ParticipantJoinedEvent {
  participantId: string;
  nomAffiche: string;
  role: ParticipantState['role'];
}

interface VoteChangedEvent {
  postItId: string;
  nombreVotes: number;
}

interface MonVoteChangedEvent {
  postItId: string;
  voteDuParticipant: boolean;
  votesRestants: number;
}

/**
 * Charge l'état d'un board, maintient une connexion SignalR au hub (contracts/realtime-hub.md)
 * et resynchronise l'état via une nouvelle lecture REST à chaque reconnexion automatique
 * (User Story 2, scénario "reconnexion").
 *
 * En React StrictMode (dev), cet effet est monté-démonté-remonté une fois : le drapeau
 * `cancelled` est vérifié à chaque point d'attente asynchrone pour garantir qu'une seule
 * connexion reste active et abonnée aux événements, quelle que soit la vitesse relative des
 * deux montages.
 */
export function useRealtimeBoard(boardId: string | undefined, participant: CurrentParticipant | null) {
  const [board, setBoard] = useState<BoardState | null>(null);
  const [error, setError] = useState<string | null>(null);
  const connectionRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    if (!boardId || !participant) {
      return;
    }

    let cancelled = false;
    let connection: HubConnection | null = null;
    const participantId = participant.participantId;

    async function resync() {
      const state = await boardsApi.getBoard(boardId!, participantId);
      if (!cancelled) {
        setBoard(state);
      }
    }

    async function connect() {
      try {
        await resync();
      } catch {
        if (!cancelled) {
          setError('Impossible de charger le board.');
        }
        return;
      }

      if (cancelled) {
        return;
      }

      connection = createRetroBoardConnection();

      connection.on('PostItAdded', ({ postIt }: PostItAddedEvent) => {
        setBoard((current) => (current ? { ...current, postIts: [...current.postIts, postIt] } : current));
      });

      connection.on('PostItUpdated', ({ postItId, texte }: PostItUpdatedEvent) => {
        setBoard((current) =>
          current
            ? { ...current, postIts: current.postIts.map((p) => (p.id === postItId ? { ...p, texte } : p)) }
            : current,
        );
      });

      connection.on('PostItMoved', ({ postItId, colonneId }: PostItMovedEvent) => {
        setBoard((current) =>
          current
            ? { ...current, postIts: current.postIts.map((p) => (p.id === postItId ? { ...p, colonneId } : p)) }
            : current,
        );
      });

      connection.on('PostItDeleted', ({ postItId }: PostItDeletedEvent) => {
        setBoard((current) =>
          current ? { ...current, postIts: current.postIts.filter((p) => p.id !== postItId) } : current,
        );
      });

      connection.on('VoteChanged', ({ postItId, nombreVotes }: VoteChangedEvent) => {
        setBoard((current) =>
          current
            ? { ...current, postIts: current.postIts.map((p) => (p.id === postItId ? { ...p, nombreVotes } : p)) }
            : current,
        );
      });

      connection.on('MonVoteChanged', ({ postItId, voteDuParticipant, votesRestants }: MonVoteChangedEvent) => {
        setBoard((current) =>
          current
            ? {
                ...current,
                mesVotesRestants: votesRestants,
                postIts: current.postIts.map((p) => (p.id === postItId ? { ...p, voteDuParticipant } : p)),
              }
            : current,
        );
      });

      connection.on('ParticipantJoined', ({ participantId: id, nomAffiche, role }: ParticipantJoinedEvent) => {
        setBoard((current) => {
          if (!current || current.participants.some((p) => p.id === id)) {
            return current;
          }
          return { ...current, participants: [...current.participants, { id, nomAffiche, role }] };
        });
      });

      connection.onreconnected(() => {
        resync().catch(() => {
          if (!cancelled) {
            setError('Reconnexion réussie mais resynchronisation impossible.');
          }
        });
        connection?.invoke('JoinBoard', boardId, participantId).catch(() => undefined);
      });

      if (cancelled) {
        connection.stop();
        return;
      }

      try {
        await connection.start();

        if (cancelled) {
          connection.stop();
          return;
        }

        await connection.invoke('JoinBoard', boardId, participantId);

        if (!cancelled) {
          connectionRef.current = connection;
        } else {
          connection.stop();
        }
      } catch {
        if (!cancelled) {
          setError('Connexion temps réel indisponible.');
        }
      }
    }

    connect();

    return () => {
      cancelled = true;
      connection?.stop();
      if (connectionRef.current === connection) {
        connectionRef.current = null;
      }
    };
  }, [boardId, participant]);

  function invoke(method: string, ...args: unknown[]) {
    connectionRef.current?.invoke(method, ...args).catch((err: Error) => setError(err.message));
  }

  return { board, error, invoke };
}
