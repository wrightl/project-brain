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

export interface ChatMemorySettings {
    recentMessageWindow: number;
    conversationSummaryInterval: number;
    maxConversationSummaryLength: number;
    enableConversationSummary: boolean;
}

export interface ChatPolicySetting {
    key: string;
    value: string;
    description?: string | null;
}

export interface ChatPolicySettings {
    policies: ChatPolicySetting[];
}

export interface MemoryFormationSettings {
    enableMemoryFormation: boolean;
    minPromotionConfidence: number;
    provisionalConfidence: number;
    activationObservationCount: number;
    maxFactsPerTurn: number;
    maxEpisodesPerTurn: number;
    maxFactsRetrieved: number;
    maxEpisodesRetrieved: number;
    indexProvisionalMemories: boolean;
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

    static async getChatMemorySettings(): Promise<ChatMemorySettings> {
        const response = await callBackendApi('/admin/settings/chat-memory');
        if (!response.ok) {
            throw new Error('Failed to fetch chat memory settings');
        }
        return response.json();
    }

    static async updateChatMemorySettings(
        settings: ChatMemorySettings
    ): Promise<ChatMemorySettings> {
        const response = await callBackendApi('/admin/settings/chat-memory', {
            method: 'PUT',
            body: settings,
        });
        if (!response.ok) {
            throw new Error('Failed to update chat memory settings');
        }
        return response.json();
    }

    static async getChatPolicySettings(): Promise<ChatPolicySettings> {
        const response = await callBackendApi('/admin/settings/chat-policies');
        if (!response.ok) {
            throw new Error('Failed to fetch chat policy settings');
        }
        return response.json();
    }

    static async updateChatPolicySettings(
        settings: ChatPolicySettings
    ): Promise<ChatPolicySettings> {
        const response = await callBackendApi('/admin/settings/chat-policies', {
            method: 'PUT',
            body: settings,
        });
        if (!response.ok) {
            throw new Error('Failed to update chat policy settings');
        }
        return response.json();
    }

    static async getMemoryFormationSettings(): Promise<MemoryFormationSettings> {
        const response = await callBackendApi('/admin/settings/memory-formation');
        if (!response.ok) {
            throw new Error('Failed to fetch memory formation settings');
        }
        return response.json();
    }

    static async updateMemoryFormationSettings(
        settings: MemoryFormationSettings
    ): Promise<MemoryFormationSettings> {
        const response = await callBackendApi('/admin/settings/memory-formation', {
            method: 'PUT',
            body: settings,
        });
        if (!response.ok) {
            throw new Error('Failed to update memory formation settings');
        }
        return response.json();
    }
}
