import { useMemo } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useRealtimeBoard } from '../hooks/useRealtimeBoard';
import { participantStorage } from '../services/participantStorage';
import { Colonne } from '../components/Colonne';

export function BoardPage() {
  const { boardId } = useParams<{ boardId: string }>();
  const participant = useMemo(() => (boardId ? participantStorage.load(boardId) : null), [boardId]);
  const { board, error, invoke } = useRealtimeBoard(boardId, participant);

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
