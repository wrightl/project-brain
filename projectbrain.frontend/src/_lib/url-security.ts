export function isSafeExternalUrl(url: string | null | undefined): boolean {
    if (!url) {
        return false;
    }

    try {
        const parsed = new URL(url);
        return parsed.protocol === 'https:' || parsed.protocol === 'http:';
    } catch {
        return false;
    }
}
