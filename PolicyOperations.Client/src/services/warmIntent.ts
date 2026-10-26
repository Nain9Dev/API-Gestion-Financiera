/**
 * Signals that a person, not a crawler, is using the page. Bots load the document but do not move
 * a pointer, touch, press keys or scroll, so waking the free-tier database only after one of these
 * keeps its monthly allowance for real visitors (spec 005).
 */
export const INTENT_EVENTS = ['pointermove', 'pointerdown', 'touchstart', 'keydown', 'scroll'] as const;

/**
 * Calls `warm` once, on the first intent signal received by `target`. Returns a function that
 * removes the listeners, for use before any intent happened.
 */
export function armWarmOnIntent(warm: () => void, target: EventTarget = window): () => void {
  const disarm = () => INTENT_EVENTS.forEach((type) => target.removeEventListener(type, onIntent));

  function onIntent() {
    disarm();
    warm();
  }

  INTENT_EVENTS.forEach((type) => target.addEventListener(type, onIntent, { passive: true }));
  return disarm;
}
