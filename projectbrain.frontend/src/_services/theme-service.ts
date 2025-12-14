import { apiClient } from '@/_lib/api-client';
import { Theme } from '@/_lib/theme-types';

export interface UpdateThemeRequest {
    theme: string;
}

export class ThemeService {
    /**
     * Get current user's theme preference
     */
    static async getTheme(): Promise<Theme> {
        try {
            const response = await apiClient<{ theme: Theme }>(
                '/api/users/me/theme'
            );
            return response.theme;
        } catch (error) {
            console.error('Error getting theme:', error);
            return 'standard';
        }
    }

    /**
     * Update user's theme preference
     */
    static async setTheme(request: UpdateThemeRequest): Promise<void> {
        try {
            await apiClient<{ theme: Theme }>('/api/users/me/theme', {
                method: 'PUT',
                body: request,
            });
        } catch (error) {
            console.error('Error setting theme:', error);
            throw error;
        }
    }
}
