import { apiClient } from './apiClient';
import type {
  BoardState,
  CreateBoardRequest,
  CreateBoardResponse,
  JoinBoardResponse,
  ThemeSummary,
} from '../types';

export const boardsApi = {
  getThemes: () => apiClient.get<ThemeSummary[]>('/api/themes'),
  createBoard: (request: CreateBoardRequest) =>
    apiClient.post<CreateBoardResponse>('/api/boards', request),
  getBoard: (boardId: string, asParticipantId?: string) =>
    apiClient.get<BoardState>(
      asParticipantId ? `/api/boards/${boardId}?asParticipantId=${asParticipantId}` : `/api/boards/${boardId}`,
    ),
  joinBoard: (boardId: string, nomAffiche: string) =>
    apiClient.post<JoinBoardResponse>(`/api/boards/${boardId}/participants`, { nomAffiche }),
};
