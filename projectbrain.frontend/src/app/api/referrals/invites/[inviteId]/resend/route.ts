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

export const POST = createApiRoute(
    async (
        req: NextRequest,
        context?: unknown
    ) => {
        // Next.js may provide `params` as a Promise (sync dynamic APIs).
        const ctx: any = await Promise.resolve(context);
        const params: any = await Promise.resolve(ctx?.params);
        const inviteId: string | undefined = params?.inviteId;
        if (!inviteId) {
            throw new Error('Invite ID is required');
        }

        const publicBaseUrl = getPublicBaseUrlFromRequest(req);
        const response = await callBackendApi(
            `/referrals/invites/${encodeURIComponent(inviteId)}/resend`,
            {
                method: 'POST',
                headers: publicBaseUrl
                    ? { 'X-Public-Base-Url': publicBaseUrl }
                    : undefined,
            }
        );

        if (!response.ok) {
            const payload = await response.json().catch(() => null);
            throw new BackendApiError(
                response.status,
                payload?.error || payload?.title || 'Failed to resend referral invite',
                payload
            );
        }

        return response.json();
    }
);

