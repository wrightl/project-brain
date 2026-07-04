import { createApiRoute } from '@/_lib/api-route-handler';
import { getAccessToken } from '@/_lib/auth';
import { NextResponse } from 'next/server';

export const dynamic = 'force-dynamic';

/**
 * Returns an access token for authenticated SignalR hub connections only.
 * Clients must not use this token for general API calls.
 */
export const GET = createApiRoute(async () => {
    const accessToken = await getAccessToken();

    if (!accessToken) {
        return NextResponse.json(
            { error: 'No access token available' },
            { status: 401 },
        );
    }

    return { token: accessToken };
});
