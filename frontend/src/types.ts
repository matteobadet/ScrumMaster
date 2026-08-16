export interface ThemeSummary {
  id: string;
  nom: string;
  colonnes: string[];
}

export interface ThemePersonnalise {
  nom: string;
  colonnes: string[];
}

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
