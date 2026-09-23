import { createAdminApiRoute } from '@/_lib/api-route-handler';
import { CommunityService } from '@/_services/community-service';

export const POST = createAdminApiRoute<void>(
    async (_req, { params }: { params: Promise<{ id: string }> }) => {
        const { id } = await params;
        await CommunityService.resolveReport(id);
    },
);
