import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { NextResponse } from 'next/server';

export const dynamic = 'force-dynamic';

/**
 * Returns a short-lived ticket that only authenticates SignalR hub connections.
 * The user's API access token never leaves the server.
 */
export const GET = createApiRoute(async () => {
    const response = await callBackendApi('/hubs/tickets', { method: 'POST' });

    if (!response.ok) {
        return NextResponse.json(
            { error: 'Unable to issue hub ticket' },
            { status: response.status },
        );
    }

    const { ticket } = (await response.json()) as { ticket: string };
    return NextResponse.json(
        { token: ticket },
        { headers: { 'Cache-Control': 'no-store' } },
    );
});
