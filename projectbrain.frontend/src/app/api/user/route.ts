import { createApiRoute } from '@/_lib/api-route-handler';
import { PagedResponse, User } from '@/_lib/types';
import { UserService } from '@/_services/user-service';

export const GET = createApiRoute<PagedResponse<User>>(async () => {
    return await UserService.getAllUsers();
});
