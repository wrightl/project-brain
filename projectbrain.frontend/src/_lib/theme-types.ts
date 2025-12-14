export type Theme = 'standard' | 'dark' | 'colourful';

export interface ThemePreferences {
    theme: Theme;
}

export function parseThemeFromPreferences(
    preferences?: string
): Theme {
    if (!preferences) return 'standard';
    
    try {
        const parsed = JSON.parse(preferences);
        if (parsed?.theme && ['standard', 'dark', 'colourful'].includes(parsed.theme)) {
            return parsed.theme as Theme;
        }
    } catch {
        // If parsing fails, check if it's a plain string
        if (['standard', 'dark', 'colourful'].includes(preferences)) {
            return preferences as Theme;
        }
    }
    
    return 'standard';
}

export function serializeThemePreferences(theme: Theme): string {
    return JSON.stringify({ theme });
}

