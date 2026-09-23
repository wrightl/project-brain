import { createApiRoute } from '@/_lib/api-route-handler';
import { CommunityService } from '@/_services/community-service';

export const PATCH = createApiRoute(
    async (
        req,
        { params }: { params: Promise<{ id: string }> },
    ) => {
        const { id } = await params;
        const body = await req.json();
        return CommunityService.updateChannel(id, body);
    },
);
