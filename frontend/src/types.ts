export interface ThemeSummary {
  id: string;
  nom: string;
  icone: string | null;
  contexte: string | null;
  colonnes: string[];
}

export interface ThemePersonnalise {
  nom: string;
  icone: string | null;
  contexte: string | null;
  colonnes: string[];
}

export type ThemeSelection =
  | { kind: 'predefined'; themeId: string }
  | { kind: 'custom'; nom: string; icone: string; contexte: string; colonnes: string[] };

export interface CreateBoardRequest {
  areaPath: string;
  iteration: string;
  themeId?: string | null;
  themePersonnalise?: ThemePersonnalise | null;
  maxVotesParParticipant?: number | null;
  nomAffiche: string;
}

export interface CreateBoardResponse {
  boardId: string;
  participantId: string;
  role: 'Facilitateur' | 'Participant';
  lienAcces: string;
}

export interface JoinBoardResponse {
  participantId: string;
  role: 'Facilitateur' | 'Participant';
}

export interface ThemeRef {
  id: string;
  nom: string;
  icone: string | null;
  contexte: string | null;
}

export interface ColonneState {
  id: string;
  intitule: string;
  ordre: number;
}

export interface PostItState {
  id: string;
  colonneId: string;
  texte: string;
  auteur: string;
  auteurParticipantId: string;
  nombreVotes: number;
  voteDuParticipant: boolean;
  workItemExporteId: number | null;
}

export interface ParticipantState {
  id: string;
  nomAffiche: string;
  role: 'Facilitateur' | 'Participant';
}

export interface BoardState {
  boardId: string;
  areaPath: string;
  iteration: string;
  statut: 'Actif' | 'Cloture';
  maxVotesParParticipant: number;
  mesVotesRestants: number | null;
  theme: ThemeRef;
  colonnes: ColonneState[];
  postIts: PostItState[];
  participants: ParticipantState[];
}

/** Identité du participant courant, conservée côté client pour la durée de la session (Assumptions spec.md). */
export interface CurrentParticipant {
  participantId: string;
  nomAffiche: string;
  role: 'Facilitateur' | 'Participant';
}

export interface AzureDevOpsConfigRequest {
  organisation: string;
  projet: string;
  pat: string;
}

export interface AzureDevOpsConfigResponse {
  areaPath: string;
  organisation: string;
  projet: string;
}

export interface EquipeAzureDevOps {
  areaPath: string;
}

export interface IterationAzureDevOps {
  cheminIteration: string;
  enCours: boolean;
}
