import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { boardsApi } from '../services/boardsApi';
import { ApiError } from '../services/apiClient';
import type { BoardSummary } from '../types';

const STATUT_LABELS: Record<BoardSummary['statut'], string> = {
  Actif: 'Actif',
  Cloture: 'Clôturé',
};

/** Historique des boards d'une équipe (specs/010-historique-boards). */
export function BoardHistoryPage() {
  const { areaPath } = useParams<{ areaPath: string }>();
  const [boards, setBoards] = useState<BoardSummary[] | null>(null);
  const [erreur, setErreur] = useState<string | null>(null);

  useEffect(() => {
    if (!areaPath) {
      return;
    }
    boardsApi
      .listerBoardsParEquipe(areaPath)
      .then(setBoards)
      .catch((err) => setErreur(err instanceof ApiError ? err.message : 'Une erreur est survenue.'));
  }, [areaPath]);

  if (!areaPath) {
    return (
      <div className="page page-narrow">
        <p>Équipe introuvable.</p>
      </div>
    );
  }

  return (
    <div className="page page-narrow">
      <h1>Historique des boards — {areaPath}</h1>
      {erreur && <p role="alert">{erreur}</p>}
      {!erreur && !boards && <p>Chargement…</p>}
      {!erreur && boards && boards.length === 0 && <p>Aucun board pour cette équipe.</p>}
      {!erreur && boards && boards.length > 0 && (
        <ul className="board-history-list">
          {boards.map((board) => (
            <li key={board.id} className="card board-history-item">
              <Link to={`/board/${board.id}`}>{board.iteration}</Link>
              <span className={`board-history-statut board-history-statut--${board.statut.toLowerCase()}`}>
                {STATUT_LABELS[board.statut]}
              </span>
              <span className="board-history-date">{new Date(board.dateCreation).toLocaleDateString('fr-FR')}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
