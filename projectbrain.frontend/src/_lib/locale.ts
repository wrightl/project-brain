/**
 * Locale detection and utilities for the application
 * Supports UK English (en-GB) and US English (en-US)
 */

export type SupportedLocale = 'en-GB' | 'en-US';

/**
 * Detects the user's locale from browser or returns default
 */
export function detectLocale(): SupportedLocale {
    if (typeof window === 'undefined') {
        return 'en-US'; // Server-side default
    }

    const browserLocale = navigator.language || (navigator as any).userLanguage || 'en-US';
    
    // Check if it's UK English
    if (browserLocale.toLowerCase().startsWith('en-gb') || browserLocale.toLowerCase() === 'en-gb') {
        return 'en-GB';
    }
    
    // Default to US English
    return 'en-US';
}

/**
 * Normalizes a locale string to a supported locale
 */
export function normalizeLocale(locale: string | null | undefined): SupportedLocale {
    if (!locale) {
        return detectLocale();
    }
    
    const normalized = locale.toLowerCase();
    if (normalized.startsWith('en-gb') || normalized === 'en-gb') {
        return 'en-GB';
    }
    
    return 'en-US';
}

/**
 * React hook for locale detection and management
 */
export function useLocale(): SupportedLocale {
    if (typeof window === 'undefined') {
        return 'en-US';
    }
    
    return detectLocale();
}

