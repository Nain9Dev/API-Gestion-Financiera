import { describe, expect, it, vi } from 'vitest';
import { armWarmOnIntent, INTENT_EVENTS } from './warmIntent';

describe('armWarmOnIntent (spec 005)', () => {
  it('does nothing until the visitor shows intent (REQ-140)', () => {
    const warm = vi.fn();
    armWarmOnIntent(warm, new EventTarget());

    expect(warm).not.toHaveBeenCalled();
  });

  it.each(['pointermove', 'pointerdown', 'touchstart', 'keydown', 'scroll'])(
    'starts the wake-up on %s (REQ-141)',
    (type) => {
      const warm = vi.fn();
      const target = new EventTarget();
      armWarmOnIntent(warm, target);

      target.dispatchEvent(new Event(type));

      expect(warm).toHaveBeenCalledTimes(1);
    }
  );

  it('lists exactly the intent signals of the spec (REQ-141)', () => {
    expect([...INTENT_EVENTS].sort()).toEqual(
      ['keydown', 'pointerdown', 'pointermove', 'scroll', 'touchstart'].sort()
    );
  });

  it('ignores every signal after the first (REQ-141)', () => {
    const warm = vi.fn();
    const target = new EventTarget();
    armWarmOnIntent(warm, target);

    target.dispatchEvent(new Event('pointermove'));
    target.dispatchEvent(new Event('keydown'));
    target.dispatchEvent(new Event('scroll'));

    expect(warm).toHaveBeenCalledTimes(1);
  });

  it('can be disarmed before any intent happens (REQ-140)', () => {
    const warm = vi.fn();
    const target = new EventTarget();
    const disarm = armWarmOnIntent(warm, target);

    disarm();
    target.dispatchEvent(new Event('pointermove'));

    expect(warm).not.toHaveBeenCalled();
  });
});
