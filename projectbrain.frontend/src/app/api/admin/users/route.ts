import { createApiRoute } from '@/_lib/api-route-handler';
import { NextRequest } from 'next/server';
import { PagedResponse, User } from '@/_lib/types';
import { UserService } from '@/_services/user-service';

export const GET = createApiRoute<PagedResponse<User>>(async (req: NextRequest) => {
    const { searchParams } = new URL(req.url);
    const pageParam = searchParams.get('page');
    const pageSizeParam = searchParams.get('pageSize');

    const options = {
        page: pageParam ? parseInt(pageParam, 10) : undefined,
        pageSize: pageSizeParam ? parseInt(pageSizeParam, 10) : undefined,
    };

    return await UserService.getAllUsers(options);
});
