import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';

export const GET = createApiRoute(async () => {
    const response = await callBackendApi('/user/data-export');
    if (!response.ok) {
        throw new Error('Failed to export user data');
    }

    const data = await response.json();
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    return new Response(JSON.stringify(data, null, 2), {
        status: 200,
        headers: {
            'Content-Type': 'application/json',
            'Content-Disposition': `attachment; filename="projectbrain-data-export-${timestamp}.json"`,
        },
    });
});
