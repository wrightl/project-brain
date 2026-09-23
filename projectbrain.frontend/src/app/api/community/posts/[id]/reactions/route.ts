import { createApiRoute } from '@/_lib/api-route-handler';
import { CommunityService } from '@/_services/community-service';

export const POST = createApiRoute<void>(
    async (_req, { params }: { params: Promise<{ id: string }> }) => {
        const { id } = await params;
        await CommunityService.addReaction(id);
    },
);

export const DELETE = createApiRoute<void>(
    async (_req, { params }: { params: Promise<{ id: string }> }) => {
        const { id } = await params;
        await CommunityService.removeReaction(id);
    },
);
