import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const DELETE = createApiRoute(async (req: NextRequest) => {
    // Extract the ID from the URL pathname
    // URL format: /api/resources/[id]
    const pathname = req.nextUrl.pathname;
    const id = pathname.split('/').pop();

    if (!id) {
        throw new Error('Resource ID is required');
    }

    const response = await callBackendApi(`/resource/${id}`, {
        method: 'DELETE',
    });

    if (!response.ok) {
        throw new Error('Failed to delete resource');
    }

    return { success: true };
});

