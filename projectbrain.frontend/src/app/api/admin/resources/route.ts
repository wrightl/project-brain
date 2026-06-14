import { createAdminApiRoute } from '@/_lib/api-route-handler';
import { Resource } from '@/_lib/types';
import { ResourceService } from '@/_services/resource-service';

export const GET = createAdminApiRoute<Resource[]>(async () => {
    return await ResourceService.getSharedResources();
});
