import { createApiRoute } from '@/_lib/api-route-handler';
import { SubscriptionService } from '@/_services/subscription-service';
import { BackendApiError } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const GET = createApiRoute(async (req: NextRequest) => {
    const sessionId =
        req.nextUrl.searchParams.get('session_id') ||
        req.nextUrl.searchParams.get('sessionId');

    if (!sessionId) {
        throw new BackendApiError(400, 'Session ID is required');
    }

    return await SubscriptionService.verifySession(sessionId);
});
