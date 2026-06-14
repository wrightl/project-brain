import { createAdminApiRoute } from '@/_lib/api-route-handler';
import { NextRequest } from 'next/server';
import { callBackendApi } from '@/_lib/backend-api';

export const GET = createAdminApiRoute(async (req: NextRequest) => {
    const pathname = req.nextUrl.pathname;
    // Path is /api/admin/users/[id]/subscription/usage
    // Split and get the user ID (4th from end)
    const parts = pathname.split('/');
    const id = parts[parts.length - 3]; // user ID (before 'subscription')

    if (!id || id === 'subscription' || id === 'usage') {
        return Response.json({ error: 'User ID is required' }, { status: 400 });
    }

    const unescapedId = decodeURIComponent(id);

    const response = await callBackendApi(
        `/admin/subscriptions/user/${unescapedId}/usage`
    );

    if (!response.ok) {
        if (response.status === 404) {
            return Response.json({ error: 'User not found' }, { status: 404 });
        }
        throw new Error('Failed to fetch user usage');
    }

    return await response.json();
});

