import { NextRequest } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { User } from '@/_lib/types';
import { UserService } from '@/_services/user-service';
import { BackendApiError } from '@/_lib/backend-api';

export const PUT = createApiRoute<User>(async (req: NextRequest) => {
    const pathname = req.nextUrl.pathname;
    // Extract ID from path like /api/user/[id]/roles
    const parts = pathname.split('/');
    const id = parts[parts.length - 2]; // Get the ID which is second to last

    if (!id) {
        throw new BackendApiError(400, 'User ID is required');
    }

    const body = await req.json();

    if (!body.roles || !Array.isArray(body.roles)) {
        throw new BackendApiError(400, 'Roles array is required');
    }

    const updatedUser = await UserService.updateUserRoles(id, body.roles);

    return updatedUser;
});
