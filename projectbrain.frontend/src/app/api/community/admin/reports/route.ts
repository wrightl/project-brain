import { createAdminApiRoute } from '@/_lib/api-route-handler';
import {
    CommunityService,
    CommunityReport,
} from '@/_services/community-service';

export const GET = createAdminApiRoute<CommunityReport[]>(async () => {
    return CommunityService.getOpenReports();
});
