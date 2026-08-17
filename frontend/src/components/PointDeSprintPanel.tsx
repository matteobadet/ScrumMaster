import { useEffect, useState } from 'react';
import { boardsApi } from '../services/boardsApi';
import { ApiError } from '../services/apiClient';
import type { PointDeSprint } from '../types';

const TYPE_LABELS: Record<string, string> = {
  Task: 'Tasks',
  UserStory: 'User Stories',
  Autres: 'Autres',
};

interface PointDeSprintPanelProps {
  boardId: string;
  participantId: string;
}

/** Panneau "Point de sprint" — statistiques Azure DevOps calculées à la demande (specs/009-sprint-review-stats). */
export function PointDeSprintPanel({ boardId, participantId }: PointDeSprintPanelProps) {
  const [stats, setStats] = useState<PointDeSprint | null>(null);
  const [erreur, setErreur] = useState<string | null>(null);
  const [chargement, setChargement] = useState(true);

  useEffect(() => {
    let annule = false;
    setChargement(true);
    setErreur(null);

    boardsApi
      .obtenirPointDeSprint(boardId, participantId)
      .then((loaded) => {
        if (!annule) {
          setStats(loaded);
        }
      })
      .catch((err) => {
        if (!annule) {
          setErreur(err instanceof ApiError ? err.message : 'Une erreur est survenue.');
        }
      })
      .finally(() => {
        if (!annule) {
          setChargement(false);
        }
      });

    return () => {
      annule = true;
    };
  }, [boardId, participantId]);

  if (chargement) {
    return (
      <div className="point-de-sprint">
        <p>Chargement du point de sprint…</p>
      </div>
    );
  }

  if (erreur) {
    return (
      <div className="point-de-sprint">
        <p role="alert">{erreur}</p>
      </div>
    );
  }

  if (!stats) {
    return null;
  }

  if (stats.repartitionParType.length === 0) {
    return (
      <div className="point-de-sprint">
        <p>Aucun work item dans l'Iteration "{stats.iteration}".</p>
      </div>
    );
  }

  return (
    <div className="point-de-sprint">
      <p className="point-de-sprint-iteration">{stats.iteration}</p>
      <div className="point-de-sprint-taux">
        {stats.totalTermine} / {stats.totalPlanifie} terminés
      </div>
      <div className="point-de-sprint-types">
        {stats.repartitionParType.map((repartition) => (
          <div key={repartition.type} className="point-de-sprint-type">
            <h3>{TYPE_LABELS[repartition.type] ?? repartition.type}</h3>
            <ul>
              <li>À faire : {repartition.aFaire}</li>
              <li>En cours : {repartition.enCours}</li>
              <li>Terminé : {repartition.termine}</li>
            </ul>
          </div>
        ))}
      </div>
    </div>
  );
}
