import { NextRequest } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { User } from '@/_lib/types';
import { UserService } from '@/_services/user-service';
import { BackendApiError } from '@/_lib/backend-api';

export const PUT = createApiRoute<User>(async (req: NextRequest) => {
    const pathname = req.nextUrl.pathname;
    const userId = pathname.split('/').pop();

    if (!userId) {
        throw new BackendApiError(400, 'User ID is required');
    }

    const body = await req.json();
    const updatedUser = await UserService.updateCurrentUser(userId, body);

    return updatedUser;
});

