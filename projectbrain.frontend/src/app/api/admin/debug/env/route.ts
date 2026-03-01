import { createApiRoute } from '@/_lib/api-route-handler';
import { hasRole } from '@/_lib/auth';
import { NextRequest, NextResponse } from 'next/server';

/** Keys we consider safe to show full value (non-secrets) */
const SAFE_KEYS = new Set([
    'NODE_ENV',
    'API_SERVER_URL',
    'APP_BASE_URL',
    'AUTH0_DOMAIN',
    'AUTH0_CLIENT_ID',
    'AUTH0_AUDIENCE',
    'AUTH0_SCOPE',
]);

/** Known env keys the app uses (server + public). Secrets are redacted. */
const KNOWN_KEYS = [
    'NODE_ENV',
    'APP_BASE_URL',
    'API_SERVER_URL',
    'AUTH0_SECRET',
    'AUTH0_DOMAIN',
    'AUTH0_CLIENT_ID',
    'AUTH0_CLIENT_SECRET',
    'AUTH0_AUDIENCE',
    'AUTH0_SCOPE',
    'NEXT_PUBLIC_GOOGLE_MAPS_API_KEY',
    'NEXT_PUBLIC_API_SERVER_URL',
    'NEXT_PUBLIC_LAUNCHDARKLY_CLIENT_ID',
    'LAUNCHDARKLY_SDK_KEY',
    'GOOGLE_MAPS_GEOCODING_API_KEY',
];

function isSafeKey(key: string): boolean {
    if (SAFE_KEYS.has(key)) return true;
    if (key.startsWith('NEXT_PUBLIC_')) return true;
    return false;
}

export const GET = createApiRoute(async (req: NextRequest) => {
    const allowed = await hasRole('admin');
    if (!allowed) {
        return NextResponse.json({ error: 'Forbidden' }, { status: 403 });
    }

    const env: Record<string, string> = {};
    const allKeys = new Set(KNOWN_KEYS);

    // Include any other NEXT_PUBLIC_* from process.env
    Object.keys(process.env).forEach((k) => {
        if (k.startsWith('NEXT_PUBLIC_')) allKeys.add(k);
    });

    allKeys.forEach((key) => {
        const value = process.env[key];
        if (value === undefined) {
            env[key] = '[NOT SET]';
        } else if (isSafeKey(key)) {
            env[key] = value;
        } else {
            env[key] = '[REDACTED]';
        }
    });

    // Sort keys for consistent display
    const sorted = Object.fromEntries(
        Object.entries(env).sort(([a], [b]) => a.localeCompare(b)),
    );

    return { env: sorted };
});
