import type { EtapeState } from '../types';

interface EtapePollPersonnaliseProps {
  etape: EtapeState;
  readOnly: boolean;
  onRepondre: (optionId: string) => void;
}

/** Étape "Poll personnalisé" (US3, specs/006-systeme-extensions-etapes). */
export function EtapePollPersonnalise({ etape, readOnly, onRepondre }: EtapePollPersonnaliseProps) {
  const totalReponses = (etape.options ?? []).reduce((total, option) => total + option.decompte, 0);

  return (
    <div className="etape-poll-personnalise">
      <h2>{etape.question}</h2>
      <ul className="poll-options" role={readOnly ? undefined : 'radiogroup'}>
        {(etape.options ?? []).map((option) => (
          <li key={option.id}>
            {readOnly ? (
              <span>{option.texte}</span>
            ) : (
              <label>
                <input
                  type="radio"
                  name={`poll-${etape.id}`}
                  checked={etape.maReponseOptionId === option.id}
                  onChange={() => onRepondre(option.id)}
                />
                {option.texte}
              </label>
            )}
            {' — '}
            {option.decompte} réponse(s)
          </li>
        ))}
      </ul>
      <p>Total : {totalReponses} réponse(s)</p>
    </div>
  );
}
