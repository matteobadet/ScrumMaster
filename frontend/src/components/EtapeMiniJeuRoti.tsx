import { useState } from 'react';
import type { EtapeState } from '../types';

const NIVEAUX: { valeur: string; emoji: string; libelle: string }[] = [
  { valeur: 'PerteDeTemps', emoji: '🗑️', libelle: 'Perte de temps' },
  { valeur: 'PeuRentable', emoji: '😕', libelle: 'Peu rentable' },
  { valeur: 'MoyennementRentable', emoji: '😐', libelle: 'Moyennement rentable' },
  { valeur: 'Rentable', emoji: '🙂', libelle: 'Rentable' },
  { valeur: 'TresRentable', emoji: '🤩', libelle: 'Très rentable' },
];

interface EtapeMiniJeuRotiProps {
  etape: EtapeState;
  readOnly: boolean;
  onRepondre: (niveau: string) => void;
}

/** Étape "ROTI" (Return On Time Invested), specs/008-roti-mini-jeu. */
export function EtapeMiniJeuRoti({ etape, readOnly, onRepondre }: EtapeMiniJeuRotiProps) {
  const [imagesEnErreur, setImagesEnErreur] = useState<Record<string, boolean>>({});

  function visuelPour(valeur: string) {
    const personnalise = (etape.visuelsRoti ?? []).find((v) => v.niveau === valeur);
    if (personnalise && !imagesEnErreur[valeur]) {
      return (
        <img
          className="roti-illustration"
          src={personnalise.urlIllustration}
          alt=""
          aria-hidden="true"
          // Repli sur l'emoji par défaut si le lien est cassé (cohérent avec Colonne.tsx, FR-010).
          onError={() => setImagesEnErreur((prev) => ({ ...prev, [valeur]: true }))}
        />
      );
    }
    return <span aria-hidden="true">{NIVEAUX.find((n) => n.valeur === valeur)?.emoji}</span>;
  }

  return (
    <div className="etape-mini-jeu">
      <h2>{etape.miniJeu?.nom ?? 'Mini-jeu'}</h2>
      {!readOnly && (
        <div className="roti-choix" role="radiogroup" aria-label="Évaluez le retour sur le temps investi">
          {NIVEAUX.map((niveau) => (
            <button
              key={niveau.valeur}
              type="button"
              aria-pressed={etape.monNiveauRoti === niveau.valeur}
              onClick={() => onRepondre(niveau.valeur)}
            >
              {visuelPour(niveau.valeur)} {niveau.libelle}
            </button>
          ))}
        </div>
      )}
      <ul className="roti-reponses">
        {(etape.reponsesRoti ?? []).map((reponse) => {
          const niveau = NIVEAUX.find((n) => n.valeur === reponse.niveau);
          return (
            <li key={reponse.participantId}>
              {reponse.nomAffiche} : {visuelPour(reponse.niveau)} {niveau?.libelle ?? reponse.niveau}
            </li>
          );
        })}
      </ul>
    </div>
  );
}
