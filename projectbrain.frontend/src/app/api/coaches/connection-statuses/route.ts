import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

type ConnectionStatusResponse = {
    status: string;
    connectionId?: string;
};

export const POST = createApiRoute(async (req: NextRequest) => {
    const body = (await req.json()) as { coachProfileIds: string[] };
    const response = await callBackendApi('/coaches/connection-statuses', {
        method: 'POST',
        body,
    });

    if (!response.ok) {
        return Response.json(
            { error: 'Failed to fetch connection statuses' },
            { status: response.status }
        );
    }

    return (await response.json()) as Record<string, ConnectionStatusResponse>;
});
