import type { EtapeState } from '../types';

const HUMEURS: { valeur: string; emoji: string; libelle: string }[] = [
  { valeur: 'Ensoleille', emoji: '☀️', libelle: 'Ensoleillé' },
  { valeur: 'Nuageux', emoji: '⛅', libelle: 'Nuageux' },
  { valeur: 'Pluvieux', emoji: '🌧️', libelle: 'Pluvieux' },
  { valeur: 'Orageux', emoji: '⛈️', libelle: 'Orageux' },
];

interface EtapeMiniJeuMeteoProps {
  etape: EtapeState;
  readOnly: boolean;
  onRepondre: (humeur: string) => void;
}

/** Étape "Météo d'équipe" (US2, specs/006-systeme-extensions-etapes). */
export function EtapeMiniJeuMeteo({ etape, readOnly, onRepondre }: EtapeMiniJeuMeteoProps) {
  return (
    <div className="etape-mini-jeu">
      <h2>{etape.miniJeu?.nom ?? 'Mini-jeu'}</h2>
      {!readOnly && (
        <div className="meteo-choix" role="radiogroup" aria-label="Choisissez votre météo">
          {HUMEURS.map((humeur) => (
            <button
              key={humeur.valeur}
              type="button"
              aria-pressed={etape.monHumeur === humeur.valeur}
              onClick={() => onRepondre(humeur.valeur)}
            >
              <span aria-hidden="true">{humeur.emoji}</span> {humeur.libelle}
            </button>
          ))}
        </div>
      )}
      <ul className="meteo-reponses">
        {(etape.reponsesMeteo ?? []).map((reponse) => {
          const humeur = HUMEURS.find((h) => h.valeur === reponse.humeur);
          return (
            <li key={reponse.participantId}>
              {reponse.nomAffiche} : <span aria-hidden="true">{humeur?.emoji ?? '❓'}</span> {humeur?.libelle ?? reponse.humeur}
            </li>
          );
        })}
      </ul>
    </div>
  );
}
