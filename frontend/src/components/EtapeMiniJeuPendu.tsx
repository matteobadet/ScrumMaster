import type { EtapeState } from '../types';

const ALPHABET = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'.split('');

interface EtapeMiniJeuPenduProps {
  etape: EtapeState;
  readOnly: boolean;
  onProposerLettre: (lettre: string) => void;
}

/** Étape "Pendu" — jeu partagé, journal de lettres append-only (specs/011-pendu-lien-externe). */
export function EtapeMiniJeuPendu({ etape, readOnly, onProposerLettre }: EtapeMiniJeuPenduProps) {
  const motMasque = etape.motMasquePendu ?? [];
  const lettresProposees = etape.lettresProposeesPendu ?? [];
  const etat = etape.etatPendu ?? 'EnCours';
  const terminee = etat !== 'EnCours';
  const lettresDejaProposees = new Set(lettresProposees.map((l) => l.lettre));

  return (
    <div className="etape-mini-jeu">
      <h2>{etape.miniJeu?.nom ?? 'Mini-jeu'}</h2>

      <div className="pendu-mot" aria-label="Mot à deviner">
        {(terminee ? (etape.motCompletPendu ?? '').split('') : motMasque).map((caractere, index) => (
          <span key={index} className={`pendu-case ${caractere ? 'pendu-case--revelee' : 'pendu-case--cachee'}`}>
            {caractere ?? ''}
          </span>
        ))}
      </div>

      <p className="pendu-essais">
        Essais restants : {etape.essaisRestantsPendu ?? 0} / {etape.maxEssaisPendu ?? 6}
      </p>

      {etat === 'Victoire' && <p role="status">🎉 Victoire ! Le mot était "{etape.motCompletPendu}".</p>}
      {etat === 'Defaite' && <p role="status">💀 Partie perdue. Le mot était "{etape.motCompletPendu}".</p>}

      {!readOnly && !terminee && (
        <div className="pendu-clavier" role="group" aria-label="Proposer une lettre">
          {ALPHABET.map((lettre) => (
            <button
              key={lettre}
              type="button"
              disabled={lettresDejaProposees.has(lettre)}
              className={
                lettresDejaProposees.has(lettre)
                  ? lettresProposees.find((l) => l.lettre === lettre)?.correcte
                    ? 'pendu-lettre pendu-lettre--correcte'
                    : 'pendu-lettre pendu-lettre--incorrecte'
                  : 'pendu-lettre'
              }
              onClick={() => onProposerLettre(lettre)}
            >
              {lettre}
            </button>
          ))}
        </div>
      )}

      {lettresProposees.length > 0 && (
        <ul className="pendu-historique">
          {lettresProposees.map((l) => (
            <li key={l.lettre}>
              {l.nomAffiche} : {l.lettre} {l.correcte ? '✓' : '✗'}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
