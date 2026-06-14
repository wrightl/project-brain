import {
    DEFAULT_THEME,
    Theme,
    isValidTheme,
} from '@/_lib/theme-registry';

export type { Theme };

export interface ThemePreferences {
    theme: Theme;
}

export function parseThemeFromPreferences(
    preferences?: string
): Theme {
    if (!preferences) return DEFAULT_THEME;

    try {
        const parsed = JSON.parse(preferences);
        if (parsed?.theme && isValidTheme(parsed.theme)) {
            return parsed.theme;
        }
    } catch {
        if (isValidTheme(preferences)) {
            return preferences;
        }
    }

    return DEFAULT_THEME;
}

export function serializeThemePreferences(theme: Theme): string {
    return JSON.stringify({ theme });
}
