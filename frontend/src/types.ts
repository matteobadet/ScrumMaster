/** Une colonne au sein d'un thème, avec son habillage visuel facultatif (specs/007-themes-visuels-colonnes). */
export interface ColonneSummaire {
  intitule: string;
  couleur: string | null;
  urlIllustration: string | null;
}

export interface ThemeSummary {
  id: string;
  nom: string;
  icone: string | null;
  contexte: string | null;
  colonnes: ColonneSummaire[];
}

export interface ThemePersonnalise {
  nom: string;
  icone: string | null;
  contexte: string | null;
  colonnes: ColonneSummaire[];
}

export type ThemeSelection =
  | { kind: 'predefined'; themeId: string }
  | { kind: 'custom'; nom: string; icone: string; contexte: string; colonnes: ColonneSummaire[] };

/** Une étape demandée à la composition d'une séquence (US1, specs/006-systeme-extensions-etapes). */
export interface EtapeRequest {
  type: 'ColonnesEtPostIts' | 'MiniJeu' | 'PollPersonnalise';
  themeId?: string | null;
  themePersonnalise?: ThemePersonnalise | null;
  miniJeuCatalogueId?: string | null;
  question?: string | null;
  options?: string[] | null;
}

export interface CreateBoardRequest {
  areaPath: string;
  iteration: string;
  themeId?: string | null;
  themePersonnalise?: ThemePersonnalise | null;
  maxVotesParParticipant?: number | null;
  nomAffiche: string;
  etapes?: EtapeRequest[] | null;
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
  couleur: string | null;
  urlIllustration: string | null;
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

export interface MiniJeuRef {
  id: string;
  nom: string;
  typeInterne: string;
}

export interface ReponseMeteo {
  participantId: string;
  nomAffiche: string;
  humeur: string;
}

export interface OptionPoll {
  id: string;
  texte: string;
  decompte: number;
}

/**
 * Une étape de la séquence du board, quel que soit son type — les champs non pertinents pour ce
 * type sont null (union étiquetée, specs/006-systeme-extensions-etapes, research.md#1).
 */
export interface EtapeState {
  id: string;
  type: 'ColonnesEtPostIts' | 'MiniJeu' | 'PollPersonnalise';
  ordre: number;
  statut: 'AVenir' | 'Active' | 'Terminee';
  theme: ThemeRef | null;
  colonnes: ColonneState[] | null;
  postIts: PostItState[] | null;
  mesVotesRestants: number | null;
  miniJeu: MiniJeuRef | null;
  reponsesMeteo: ReponseMeteo[] | null;
  monHumeur: string | null;
  question: string | null;
  options: OptionPoll[] | null;
  maReponseOptionId: string | null;
}

export interface BoardState {
  boardId: string;
  areaPath: string;
  iteration: string;
  statut: 'Actif' | 'Cloture';
  maxVotesParParticipant: number;
  etapes: EtapeState[];
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
