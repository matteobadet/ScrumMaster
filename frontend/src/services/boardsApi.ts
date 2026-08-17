import { apiClient } from './apiClient';
import type {
  AzureDevOpsConfigRequest,
  AzureDevOpsConfigResponse,
  BoardState,
  CreateBoardRequest,
  CreateBoardResponse,
  EquipeAzureDevOps,
  IterationAzureDevOps,
  JoinBoardResponse,
  MiniJeuRef,
  PointDeSprint,
  ThemeSummary,
} from '../types';

export const boardsApi = {
  getThemes: () => apiClient.get<ThemeSummary[]>('/api/themes'),
  getMiniJeux: () => apiClient.get<MiniJeuRef[]>('/api/mini-jeux'),
  createBoard: (request: CreateBoardRequest) =>
    apiClient.post<CreateBoardResponse>('/api/boards', request),
  getBoard: (boardId: string, asParticipantId?: string) =>
    apiClient.get<BoardState>(
      asParticipantId ? `/api/boards/${boardId}?asParticipantId=${asParticipantId}` : `/api/boards/${boardId}`,
    ),
  joinBoard: (boardId: string, nomAffiche: string) =>
    apiClient.post<JoinBoardResponse>(`/api/boards/${boardId}/participants`, { nomAffiche }),
  configurerAzureDevOps: (areaPath: string, request: AzureDevOpsConfigRequest) =>
    apiClient.put<AzureDevOpsConfigResponse>(`/api/equipes/${areaPath}/azure-devops-config`, request),
  listerEquipesAvecAzureDevOps: () => apiClient.get<EquipeAzureDevOps[]>('/api/equipes/avec-azure-devops'),
  obtenirIterationsAzureDevOps: (areaPath: string) =>
    apiClient.get<IterationAzureDevOps[]>(`/api/equipes/${areaPath}/azure-devops/iterations`),
  obtenirPointDeSprint: (boardId: string, participantId: string) =>
    apiClient.get<PointDeSprint>(`/api/boards/${boardId}/point-de-sprint?asParticipantId=${participantId}`),
};
