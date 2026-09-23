import { createApiRoute } from '@/_lib/api-route-handler';
import { CommunityService, CommunityChannel } from '@/_services/community-service';

export const GET = createApiRoute<CommunityChannel[]>(async () => {
    return CommunityService.getChannels();
});
