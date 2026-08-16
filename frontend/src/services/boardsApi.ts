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
  getBoard: (boardId: string) => apiClient.get<BoardState>(`/api/boards/${boardId}`),
  joinBoard: (boardId: string, nomAffiche: string) =>
    apiClient.post<JoinBoardResponse>(`/api/boards/${boardId}/participants`, { nomAffiche }),
};
