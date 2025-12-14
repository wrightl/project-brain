import { createApiRoute } from '@/_lib/api-route-handler';
import { Resource, PagedResponse } from '@/_lib/types';
import { ResourceService } from '@/_services/resource-service';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<PagedResponse<Resource>>(async (req: NextRequest) => {
    const { searchParams } = new URL(req.url);
    const shared = searchParams.get('shared') === 'true';

    if (shared) {
        // Shared resources don't have pagination yet
        const resources = await ResourceService.getSharedResources();
        return {
            items: resources,
            page: 1,
            pageSize: resources.length,
            totalCount: resources.length,
            totalPages: 1,
        } as PagedResponse<Resource>;
    }

    const pageParam = searchParams.get('page');
    const pageSizeParam = searchParams.get('pageSize');

    const options = {
        page: pageParam ? parseInt(pageParam, 10) : undefined,
        pageSize: pageSizeParam ? parseInt(pageSizeParam, 10) : undefined,
    };

    return await ResourceService.getResources(options);
});
