export function isSafeExternalUrl(url: string | null | undefined): boolean {
    if (!url) {
        return false;
    }

    try {
        const parsed = new URL(url);
        if (parsed.protocol === 'https:') {
            return true;
        }

        return (
            parsed.protocol === 'http:' &&
            (parsed.hostname === 'localhost' || parsed.hostname === '127.0.0.1')
        );
    } catch {
        return false;
    }
}

export function isAllowedCheckoutRedirectUrl(url: string | null | undefined): boolean {
    if (!url) {
        return false;
    }

    try {
        const parsed = new URL(url);
        if (parsed.protocol !== 'https:') {
            return false;
        }

        return (
            parsed.hostname === 'checkout.stripe.com' ||
            parsed.hostname.endsWith('.stripe.com')
        );
    } catch {
        return false;
    }
}
