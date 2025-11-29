import { createApiRoute } from '@/_lib/api-route-handler';
import { User } from '@/_lib/types';
import { UserService } from '@/_services/user-service';

export const GET = createApiRoute<User[]>(async () => {
    return await UserService.getAllUsers();
});
