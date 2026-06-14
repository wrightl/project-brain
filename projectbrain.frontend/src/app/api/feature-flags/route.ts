import { createApiRoute } from '@/_lib/api-route-handler';
import { getAccessToken } from '@/_lib/auth';
import { NextRequest } from 'next/server';

export const GET = createApiRoute(async () => {
    const accessToken = await getAccessToken();
    const apiServerUrl =
        process.env.API_SERVER_URL || 'https://localhost:7585';

    const response = await fetch(`${apiServerUrl}/feature-flags`, {
        method: 'GET',
        headers: {
            Authorization: `Bearer ${accessToken}`,
            'Content-Type': 'application/json',
        },
    });

    if (!response.ok) {
        return Response.json(
            { error: 'Failed to fetch feature flags' },
            { status: response.status }
        );
    }

    return await response.json();
});
