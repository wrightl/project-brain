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

