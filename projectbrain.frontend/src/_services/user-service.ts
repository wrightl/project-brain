import { callBackendApi } from '@/_lib/backend-api';
import { User } from '@/_lib/types';

export class UserService {
    /**
     * Get current user
     */
    static async getCurrentUser(): Promise<User> {
        const response = callBackendApi('/users/me');
        return (await response).json();
    }
}
