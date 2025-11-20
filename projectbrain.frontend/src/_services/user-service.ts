import { callBackendApi } from '@/_lib/backend-api';
import { User } from '@/_lib/types';

export class UserService {
    /**
     * Get current user
     */
    static async getCurrentUser(): Promise<User | null> {
        const response = await callBackendApi('/users/me');

        if (response.status === 404) {
            return null;
        }
        return response.json();
    }
}
