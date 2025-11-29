import { createApiRoute } from '@/_lib/api-route-handler';
import { CoachService } from '@/_services/coach-service';
import { BackendApiError } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const POST = createApiRoute(async (req: NextRequest) => {
    const pathname = req.nextUrl.pathname;
    // Extract userId from pathname: /api/coach/clients/[userId]/accept
    const pathParts = pathname.split('/');
    const clientsIndex = pathParts.indexOf('clients');
    const userId = pathParts[clientsIndex + 1];

    if (!userId || userId === 'accept') {
        throw new BackendApiError(400, 'User ID is required');
    }

    await CoachService.acceptClientConnection(userId);
    return { success: true };
});

