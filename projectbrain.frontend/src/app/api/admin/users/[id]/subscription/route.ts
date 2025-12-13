import { createApiRoute } from '@/_lib/api-route-handler';
import { NextRequest } from 'next/server';
import { callBackendApi } from '@/_lib/backend-api';

export const GET = createApiRoute(async (req: NextRequest) => {
    const pathname = req.nextUrl.pathname;
    // Path is /api/admin/users/[id]/subscription
    // Split and get the user ID (4th from end)
    const parts = pathname.split('/');
    const id = parts[parts.length - 2]; // Get user ID (before 'subscription')

    if (!id || id === 'subscription') {
        return Response.json({ error: 'User ID is required' }, { status: 400 });
    }

    // Call backend API to get user subscription (admin endpoint)
    const response = await callBackendApi(`/admin/subscriptions/user/${id}`);
    
    if (!response.ok) {
        if (response.status === 404) {
            // User has no subscription - return default
            return Response.json({
                tier: 'Free',
                status: 'active',
                userType: 'user',
                isExcluded: false,
            });
        }
        throw new Error('Failed to fetch user subscription');
    }

    return await response.json();
});

