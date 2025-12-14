import { callBackendApi } from '@/_lib/backend-api';
import { Theme } from '@/_lib/theme-types';
import { Coach, PagedResponse, User } from '@/_lib/types';

export class UserService {
    /**
     * Get current user
     */
    static async getCurrentUser(): Promise<User | Coach | null> {
        const response = await callBackendApi('/users/me');

        if (response.status === 404) {
            return null;
        }
        return response.json();
    }

    /**
     * Get all users (admin only)
     */
    static async getAllUsers(options?: {
        page?: number;
        pageSize?: number;
    }): Promise<PagedResponse<User>> {
        const params = new URLSearchParams();
        if (options?.page) {
            params.append('page', options.page.toString());
        }
        if (options?.pageSize) {
            params.append('pageSize', options.pageSize.toString());
        }
        const queryParam = params.toString() ? `?${params.toString()}` : '';
        const response = await callBackendApi(`/usermanagement${queryParam}`);
        if (!response.ok) {
            throw new Error('Failed to fetch users');
        }
        return response.json();
    }

    /**
     * Get user by ID (admin only)
     */
    static async getUserById(id: string): Promise<User | null> {
        const response = await callBackendApi(`/usermanagement/${id}`);
        if (response.status === 404) {
            return null;
        }
        if (!response.ok) {
            throw new Error('Failed to fetch user');
        }
        return response.json();
    }

    /**
     * Update user (admin only)
     */
    static async updateUser(id: string, updates: Partial<User>): Promise<User> {
        const response = await callBackendApi(`/usermanagement/${id}`, {
            method: 'PUT',
            body: updates,
        });
        if (!response.ok) {
            throw new Error('Failed to update user');
        }
        return response.json();
    }

    /**
     * Update user roles (admin only)
     */
    static async updateUserRoles(id: string, roles: string[]): Promise<User> {
        const response = await callBackendApi(`/usermanagement/${id}/roles`, {
            method: 'PUT',
            body: { roles: roles },
        });
        if (!response.ok) {
            throw new Error('Failed to update user roles');
        }
        return response.json();
    }

    /**
     * Delete user (admin only)
     */
    static async deleteUser(id: string): Promise<void> {
        const response = await callBackendApi(`/usermanagement/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) {
            throw new Error('Failed to delete user');
        }
    }

    /**
     * Update current user profile
     */
    static async updateCurrentUser(
        userId: string,
        updates: {
            fullName?: string;
            doB?: string;
            preferredPronoun?: string;
            neurodiverseTraits?: string[];
            preferences?: string;
            streetAddress?: string;
            addressLine2?: string;
            city?: string;
            stateProvince?: string;
            postalCode?: string;
            country?: string;
        }
    ): Promise<User> {
        const response = await callBackendApi(`/users/me/${userId}`, {
            method: 'PUT',
            body: updates,
        });
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Failed to update user');
        }
        return response.json();
    }

    /** Update current user theme */
    static async updateCurrentUserTheme(
        theme: Theme
    ): Promise<{ theme: Theme }> {
        const response = await callBackendApi(`/users/me/theme`, {
            method: 'PUT',
            body: { theme },
        });
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Failed to update user theme');
        }
        return response.json() as Promise<{ theme: Theme }>;
    }

    /** Get current user theme */
    static async getCurrentUserTheme(): Promise<{ theme: Theme }> {
        const response = await callBackendApi(`/users/me/theme`);
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Failed to get user theme');
        }
        return response.json() as unknown as { theme: Theme };
    }
}
