import { createApiRoute } from '@/_lib/api-route-handler';
import { CommunityService } from '@/_services/community-service';
import { BackendApiError } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const POST = createApiRoute<void>(
    async (
        req: NextRequest,
        { params }: { params: Promise<{ id: string }> },
    ) => {
        const { id } = await params;
        const body = await req.json();
        if (!body?.reason || typeof body.reason !== 'string') {
            throw new BackendApiError(400, 'Reason is required');
        }
        await CommunityService.reportPost(id, body.reason);
    },
);
