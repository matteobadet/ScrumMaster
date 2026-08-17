import { useEffect, useRef, useState } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { createRetroBoardConnection } from '../services/realtimeClient';
import { boardsApi } from '../services/boardsApi';
import type { BoardState, CurrentParticipant, EtapeState, ParticipantState, PostItState } from '../types';

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

interface PostItExportedEvent {
  postItId: string;
  workItemId: number;
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

interface ReponseMiniJeuChangeeEvent {
  etapeId: string;
  participantId: string;
  nomAffiche: string;
  reponse: string;
}

interface ReponsePollPersonnaliseChangeeEvent {
  etapeId: string;
  decompteParOption: { optionId: string; decompte: number }[];
}

/**
 * Applique une mise à jour à l'étape "Colonnes et post-its" active du board (au plus une à la
 * fois, specs/006-systeme-extensions-etapes) — les événements post-its/votes ne portent pas
 * d'etapeId, ils s'appliquent implicitement à cette étape (contracts/realtime-hub-delta.md).
 */
function updateActiveColonnesEtape(board: BoardState, update: (etape: EtapeState) => EtapeState): BoardState {
  return {
    ...board,
    etapes: board.etapes.map((etape) =>
      etape.statut === 'Active' && etape.type === 'ColonnesEtPostIts' ? update(etape) : etape,
    ),
  };
}

/** Applique une mise à jour à une étape précise du board, par id. */
function updateEtapeById(board: BoardState, etapeId: string, update: (etape: EtapeState) => EtapeState): BoardState {
  return { ...board, etapes: board.etapes.map((etape) => (etape.id === etapeId ? update(etape) : etape)) };
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
        setBoard((current) =>
          current
            ? updateActiveColonnesEtape(current, (etape) => ({ ...etape, postIts: [...(etape.postIts ?? []), postIt] }))
            : current,
        );
      });

      connection.on('PostItUpdated', ({ postItId, texte }: PostItUpdatedEvent) => {
        setBoard((current) =>
          current
            ? updateActiveColonnesEtape(current, (etape) => ({
                ...etape,
                postIts: (etape.postIts ?? []).map((p) => (p.id === postItId ? { ...p, texte } : p)),
              }))
            : current,
        );
      });

      connection.on('PostItMoved', ({ postItId, colonneId }: PostItMovedEvent) => {
        setBoard((current) =>
          current
            ? updateActiveColonnesEtape(current, (etape) => ({
                ...etape,
                postIts: (etape.postIts ?? []).map((p) => (p.id === postItId ? { ...p, colonneId } : p)),
              }))
            : current,
        );
      });

      connection.on('PostItDeleted', ({ postItId }: PostItDeletedEvent) => {
        setBoard((current) =>
          current
            ? updateActiveColonnesEtape(current, (etape) => ({
                ...etape,
                postIts: (etape.postIts ?? []).filter((p) => p.id !== postItId),
              }))
            : current,
        );
      });

      connection.on('PostItExported', ({ postItId, workItemId }: PostItExportedEvent) => {
        setBoard((current) =>
          current
            ? updateActiveColonnesEtape(current, (etape) => ({
                ...etape,
                postIts: (etape.postIts ?? []).map((p) =>
                  p.id === postItId ? { ...p, workItemExporteId: workItemId } : p,
                ),
              }))
            : current,
        );
      });

      connection.on('VoteChanged', ({ postItId, nombreVotes }: VoteChangedEvent) => {
        setBoard((current) =>
          current
            ? updateActiveColonnesEtape(current, (etape) => ({
                ...etape,
                postIts: (etape.postIts ?? []).map((p) => (p.id === postItId ? { ...p, nombreVotes } : p)),
              }))
            : current,
        );
      });

      connection.on('MonVoteChanged', ({ postItId, voteDuParticipant, votesRestants }: MonVoteChangedEvent) => {
        setBoard((current) =>
          current
            ? updateActiveColonnesEtape(current, (etape) => ({
                ...etape,
                mesVotesRestants: votesRestants,
                postIts: (etape.postIts ?? []).map((p) => (p.id === postItId ? { ...p, voteDuParticipant } : p)),
              }))
            : current,
        );
      });

      connection.on('ThemeChanged', () => {
        // Le nouveau thème porte de nouvelles colonnes (et les post-its existants ont été
        // réaffectés côté serveur) : une resynchronisation complète est plus sûre qu'un patch.
        resync().catch(() => {
          if (!cancelled) {
            setError('Le thème a changé mais la resynchronisation a échoué.');
          }
        });
      });

      connection.on('BoardClosed', () => {
        // La dernière étape passe aussi à Terminee côté serveur (EtapeService.AvancerEtapeAsync) :
        // une resynchronisation complète évite de dupliquer cette logique côté client.
        resync().catch(() => {
          if (!cancelled) {
            setError('Le board a été clôturé mais la resynchronisation a échoué.');
          }
        });
      });

      connection.on('EtapeChangee', () => {
        // La nouvelle étape active porte son propre état (thème/colonnes ou mini-jeu ou poll) :
        // une resynchronisation complète est plus sûre qu'un patch (US1, T017-T018).
        resync().catch(() => {
          if (!cancelled) {
            setError("L'étape a changé mais la resynchronisation a échoué.");
          }
        });
      });

      connection.on(
        'ReponseMiniJeuChangee',
        ({ etapeId, participantId: repondantId, nomAffiche, reponse }: ReponseMiniJeuChangeeEvent) => {
          setBoard((current) =>
            current
              ? updateEtapeById(current, etapeId, (etape) =>
                  // Le type de mini-jeu (Météo/ROTI) détermine quel champ de la réponse patcher
                  // (union étiquetée, specs/008-roti-mini-jeu) — sinon la réponse est bien
                  // enregistrée côté serveur mais n'apparaît jamais tant que le board n'est pas
                  // resynchronisé manuellement.
                  etape.miniJeu?.typeInterne === 'roti'
                    ? {
                        ...etape,
                        reponsesRoti: [
                          ...(etape.reponsesRoti ?? []).filter((r) => r.participantId !== repondantId),
                          { participantId: repondantId, nomAffiche, niveau: reponse },
                        ],
                        monNiveauRoti: repondantId === participantId ? reponse : etape.monNiveauRoti,
                      }
                    : {
                        ...etape,
                        reponsesMeteo: [
                          ...(etape.reponsesMeteo ?? []).filter((r) => r.participantId !== repondantId),
                          { participantId: repondantId, nomAffiche, humeur: reponse },
                        ],
                        monHumeur: repondantId === participantId ? reponse : etape.monHumeur,
                      },
                )
              : current,
          );
        },
      );

      connection.on('ReponsePollPersonnaliseChangee', ({ etapeId, decompteParOption }: ReponsePollPersonnaliseChangeeEvent) => {
        setBoard((current) =>
          current
            ? updateEtapeById(current, etapeId, (etape) => ({
                ...etape,
                options: (etape.options ?? []).map((option) => {
                  const decompte = decompteParOption.find((d) => d.optionId === option.id);
                  return decompte ? { ...option, decompte: decompte.decompte } : option;
                }),
              }))
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

  /**
   * L'événement `ReponsePollPersonnaliseChangee` ne porte que le décompte agrégé (visible par
   * tous), pas l'identité du répondant — la réponse du participant courant est donc appliquée
   * localement en optimiste, avant confirmation serveur (contracts/realtime-hub-delta.md).
   */
  function repondrePollPersonnalise(boardId: string, etapeId: string, optionId: string) {
    setBoard((current) => (current ? updateEtapeById(current, etapeId, (etape) => ({ ...etape, maReponseOptionId: optionId })) : current));
    invoke('RepondrePollPersonnalise', boardId, etapeId, optionId);
  }

  return { board, error, invoke, repondrePollPersonnalise };
}
