/**
 * Validates if a string is a valid GUID/UUID
 * @param guid - The string to validate
 * @returns true if the string is a valid GUID, false otherwise
 */
export function isValidGuid(guid: string): boolean {
    if (!guid || typeof guid !== 'string') {
        return false;
    }
    
    // GUID format: 8-4-4-4-12 hexadecimal digits
    const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    return guidRegex.test(guid);
}

/** Converts PascalCase, snake_case, or kebab-case strings to sentence case. */
export function toSentenceCase(value: string): string {
    const spaced = value
        .replace(/([a-z])([A-Z])/g, '$1 $2')
        .replace(/[_-]+/g, ' ')
        .trim()
        .replace(/\s+/g, ' ');

    const lower = spaced.toLowerCase();
    return lower.charAt(0).toUpperCase() + lower.slice(1);
}

