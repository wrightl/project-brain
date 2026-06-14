import { createApiRoute } from '@/_lib/api-route-handler';
import { requireRole } from '@/_lib/auth';
import { AppRoles } from '@/_lib/roles';
import { PagedResponse, User } from '@/_lib/types';
import { UserService } from '@/_services/user-service';
import { NextResponse } from 'next/server';

export const GET = createApiRoute<PagedResponse<User>>(async () => {
    const allowed = await requireRole(AppRoles.Admin);
    if (!allowed) {
        return NextResponse.json({ error: 'Forbidden' }, { status: 403 });
    }

    return await UserService.getAllUsers();
});
