import { createApiRoute } from '@/_lib/api-route-handler';
import { User } from '@/_lib/types';
import { UserService } from '@/_services/user-service';
import { BackendApiError } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<User>(async (req: NextRequest) => {
    const user = (await UserService.getCurrentUser()) as User;
    if (!user) {
        throw new BackendApiError(404, 'User not found');
    }
    return user;
});
