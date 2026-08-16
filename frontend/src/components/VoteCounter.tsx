interface VoteCounterProps {
  nombreVotes: number;
  aVote: boolean;
  votesRestants: number | null;
  onVote: () => void;
  onRemoveVote: () => void;
}

export function VoteCounter({ nombreVotes, aVote, votesRestants, onVote, onRemoveVote }: VoteCounterProps) {
  const quotaAtteint = votesRestants !== null && votesRestants <= 0;

  return (
    <div className="vote-counter">
      <span>{nombreVotes} vote(s)</span>
      {aVote ? (
        <button type="button" onClick={onRemoveVote}>
          Retirer mon vote
        </button>
      ) : (
        <button type="button" onClick={onVote} disabled={quotaAtteint}>
          Voter{votesRestants !== null ? ` (${votesRestants} restant(s))` : ''}
        </button>
      )}
    </div>
  );
}
