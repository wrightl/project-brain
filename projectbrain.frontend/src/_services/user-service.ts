import { callBackendApi } from '@/_lib/backend-api';
import { Coach, User } from '@/_lib/types';

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
    static async getAllUsers(): Promise<User[]> {
        const response = await callBackendApi('/usermanagement');
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
            body: JSON.stringify(updates),
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
            body: JSON.stringify(updates),
        });
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Failed to update user');
        }
        return response.json();
    }
}
