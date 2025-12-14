import { createApiRoute } from '@/_lib/api-route-handler';
import { ConnectionService, Connection } from '@/_services/connection-service';
import { PagedResponse } from '@/_lib/types';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<PagedResponse<Connection>>(async (req: NextRequest) => {
    const { searchParams } = new URL(req.url);
    const pageParam = searchParams.get('page');
    const pageSizeParam = searchParams.get('pageSize');

    const options = {
        page: pageParam ? parseInt(pageParam, 10) : undefined,
        pageSize: pageSizeParam ? parseInt(pageSizeParam, 10) : undefined,
    };

    return await ConnectionService.getConnections(options);
});

