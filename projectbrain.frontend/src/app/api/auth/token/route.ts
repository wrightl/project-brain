import { createApiRoute } from '@/_lib/api-route-handler';
import { getAccessToken } from '@/_lib/auth';
import { NextResponse } from 'next/server';

export const dynamic = 'force-dynamic';

/**
 * Returns a short-lived access token for SignalR hub connections.
 * Requires an authenticated session via createApiRoute.
 */
export const GET = createApiRoute(async () => {
    const accessToken = await getAccessToken();

    if (!accessToken) {
        return NextResponse.json(
            { error: 'No access token available' },
            { status: 401 }
        );
    }

    return { token: accessToken };
});
