export const THEMES = [
    {
        id: 'standard',
        label: 'Standard',
        description: 'The default color scheme',
    },
    {
        id: 'dark',
        label: 'Dark',
        description: 'A dark color theme for low-light environments',
    },
    {
        id: 'colourful',
        label: 'Colourful',
        description: 'A vibrant and colorful color scheme',
    },
    {
        id: 'dotdash',
        label: 'Dot + Dash',
        description: 'Brand theme inspired by Dot + Dash Consulting',
    },
] as const;

export type Theme = (typeof THEMES)[number]['id'];

export const THEME_IDS: readonly Theme[] = THEMES.map((theme) => theme.id);

export const DEFAULT_THEME: Theme = 'standard';

export function isValidTheme(value: unknown): value is Theme {
    return (
        typeof value === 'string' &&
        THEME_IDS.includes(value as Theme)
    );
}
