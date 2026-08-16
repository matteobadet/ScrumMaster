import type { CurrentParticipant } from '../types';

const storageKey = (boardId: string) => `scrummaster:participant:${boardId}`;

export const participantStorage = {
  save(boardId: string, participant: CurrentParticipant): void {
    sessionStorage.setItem(storageKey(boardId), JSON.stringify(participant));
  },
  load(boardId: string): CurrentParticipant | null {
    const raw = sessionStorage.getItem(storageKey(boardId));
    return raw ? (JSON.parse(raw) as CurrentParticipant) : null;
  },
};
