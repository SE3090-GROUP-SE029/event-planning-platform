import { describe, expect, it } from 'vitest';
import { tokens } from '../src/shared/theme/tokens';
import { pastelTheme } from '../src/shared/theme/pastelTheme';

describe('Pastel Theme & Design Tokens', () => {
  it('defines correct core pastel and canvas color tokens', () => {
    expect(tokens.colors.canvas).toBe('#F7F3E9');
    expect(tokens.colors.obsidian).toBe('#19191C');
    expect(tokens.colors.pastelPink).toBe('#F9BFD8');
    expect(tokens.colors.pastelYellow).toBe('#FEE388');
    expect(tokens.colors.pastelGreen).toBe('#C4DDB8');
    expect(tokens.colors.pastelBlue).toBe('#BCD8F0');
    expect(tokens.colors.pastelPinkLight).toBe('#FDEEF5');
    expect(tokens.colors.pastelGreenLight).toBe('#F0F6EC');
    expect(tokens.colors.pastelBlueLight).toBe('#EEF5FC');
    expect(tokens.colors.pastelPeachLight).toBe('#FFF1E6');
    expect(tokens.colors.pastelLavenderLight).toBe('#F7F2FD');
  });

  it('configures MUI pastel theme with 20px card border radius and pastel blue primary', () => {
    expect(pastelTheme.palette.background.default).toBe('#F7F3E9');
    expect(pastelTheme.palette.primary.main).toBe(tokens.colors.pastelBlue);
    expect(pastelTheme.shape.borderRadius).toBe(20);
  });
});
