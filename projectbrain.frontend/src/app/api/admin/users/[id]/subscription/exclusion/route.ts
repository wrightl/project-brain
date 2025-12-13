import { createApiRoute } from '@/_lib/api-route-handler';
import { NextRequest } from 'next/server';
import { callBackendApi } from '@/_lib/backend-api';

export const POST = createApiRoute(async (req: NextRequest) => {
    const pathname = req.nextUrl.pathname;
    // Path is /api/admin/users/[id]/subscription/exclusion
    // Split and get the user ID (3rd from end)
    const parts = pathname.split('/');
    const id = parts[parts.length - 3]; // Get user ID (before 'subscription')

    if (!id || id === 'subscription' || id === 'exclusion') {
        return Response.json({ error: 'User ID is required' }, { status: 400 });
    }

    const unescapedId = decodeURIComponent(id);

    const body = await req.json();
    const { userType = 'user', notes } = body;

    // Call backend API to add exclusion
    const response = await callBackendApi('/admin/subscriptions/exclusions', {
        method: 'POST',
        body: {
            userId: unescapedId,
            userType,
            notes,
        },
    });

    if (!response.ok) {
        throw new Error('Failed to add exclusion');
    }

    return await response.json();
});

export const DELETE = createApiRoute(async (req: NextRequest) => {
    const pathname = req.nextUrl.pathname;
    // Path is /api/admin/users/[id]/subscription/exclusion
    // Split and get the user ID (3rd from end)
    const parts = pathname.split('/');
    const id = parts[parts.length - 3]; // Get user ID (before 'subscription')

    if (!id || id === 'subscription' || id === 'exclusion') {
        return Response.json({ error: 'User ID is required' }, { status: 400 });
    }

    // Call backend API to remove exclusion
    const response = await callBackendApi(
        `/admin/subscriptions/exclusions/${id}`,
        {
            method: 'DELETE',
        }
    );

    if (!response.ok) {
        throw new Error('Failed to remove exclusion');
    }

    return await response.json();
});
