interface VoteCounterProps {
  nombreVotes: number;
  aVote: boolean;
  votesRestants: number | null;
  readOnly: boolean;
  onVote: () => void;
  onRemoveVote: () => void;
}

export function VoteCounter({ nombreVotes, aVote, votesRestants, readOnly, onVote, onRemoveVote }: VoteCounterProps) {
  const quotaAtteint = votesRestants !== null && votesRestants <= 0;

  if (readOnly) {
    return (
      <div className="vote-counter">
        <span>{nombreVotes} vote(s)</span>
      </div>
    );
  }

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
