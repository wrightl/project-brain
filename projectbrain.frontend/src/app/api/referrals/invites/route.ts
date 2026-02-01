import { createApiRoute } from '@/_lib/api-route-handler';
import { NextRequest } from 'next/server';
import { BackendApiError, callBackendApi } from '@/_lib/backend-api';

function getPublicBaseUrlFromRequest(req: NextRequest): string | null {
    const protoRaw =
        req.headers.get('x-forwarded-proto') ||
        req.nextUrl.protocol ||
        'https:';
    const proto = (protoRaw.split(',')[0]?.trim() || 'https').replace(/:$/, '');

    const hostRaw =
        req.headers.get('x-forwarded-host') ||
        req.headers.get('host') ||
        req.nextUrl.host;
    const host = (hostRaw?.split(',')[0]?.trim() || '').trim();

    if (!host) return null;
    return `${proto}://${host}`;
}

export const GET = createApiRoute(async () => {
    const response = await callBackendApi('/referrals/invites');
    if (!response.ok) {
        const payload = await response.json().catch(() => null);
        throw new BackendApiError(
            response.status,
            payload?.error || payload?.title || 'Failed to fetch referral invites',
            payload
        );
    }
    return response.json();
});

export const POST = createApiRoute(async (req: NextRequest) => {
    const body = await req.json();
    const publicBaseUrl = getPublicBaseUrlFromRequest(req);
    const response = await callBackendApi('/referrals/invites', {
        method: 'POST',
        body,
        headers: publicBaseUrl
            ? { 'X-Public-Base-Url': publicBaseUrl }
            : undefined,
    });
    if (!response.ok) {
        const payload = await response.json().catch(() => null);
        throw new BackendApiError(
            response.status,
            payload?.error || payload?.title || 'Failed to create referral invites',
            payload
        );
    }
    return response.json();
});

