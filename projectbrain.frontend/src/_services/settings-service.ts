import { callBackendApi } from '@/_lib/backend-api';

export interface ApplicationSetting {
    key: string;
    value: string;
    category?: string;
    description?: string;
    updatedAt: string;
    updatedBy: string;
}

export interface AISettings {
    maxSearchResults: number;
    maxContentLengthPerSource: number;
    maxHistoryMessages: number;
    maxTotalTokens: number;
}

export interface UpdateAISettingsRequest {
    maxSearchResults: number;
    maxContentLengthPerSource: number;
    maxHistoryMessages: number;
    maxTotalTokens: number;
}

export class SettingsService {
    /**
     * Get all settings (admin only)
     */
    static async getAllSettings(): Promise<ApplicationSetting[]> {
        const response = await callBackendApi('/admin/settings');
        if (!response.ok) {
            throw new Error('Failed to fetch settings');
        }
        return response.json();
    }

    /**
     * Get settings by category (admin only)
     */
    static async getSettingsByCategory(category: string): Promise<ApplicationSetting[]> {
        const response = await callBackendApi(`/admin/settings/category/${category}`);
        if (!response.ok) {
            throw new Error('Failed to fetch settings');
        }
        return response.json();
    }

    /**
     * Get AI settings (admin only)
     */
    static async getAISettings(): Promise<AISettings> {
        const response = await callBackendApi('/admin/settings/ai');
        if (!response.ok) {
            throw new Error('Failed to fetch AI settings');
        }
        return response.json();
    }

    /**
     * Update a setting (admin only)
     */
    static async updateSetting(key: string, value: string): Promise<void> {
        const response = await callBackendApi(`/admin/settings/${key}`, {
            method: 'PUT',
            body: { value },
        });
        if (!response.ok) {
            throw new Error('Failed to update setting');
        }
    }

    /**
     * Update AI settings (admin only)
     */
    static async updateAISettings(settings: UpdateAISettingsRequest): Promise<AISettings> {
        const response = await callBackendApi('/admin/settings/ai', {
            method: 'PUT',
            body: settings,
        });
        if (!response.ok) {
            throw new Error('Failed to update AI settings');
        }
        return response.json();
    }
}
