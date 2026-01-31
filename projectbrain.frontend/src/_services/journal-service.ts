import { callBackendApi } from '@/_lib/backend-api';
import type { PagedResponse } from '@/_lib/types';

export interface JournalEntry {
    id: string;
    userId: string;
    content: string;
    summary: string | null;
    createdAt: string;
    updatedAt: string;
    tags?: JournalTag[];
    systemTags?: JournalEntrySystemTag[];
}

export interface JournalTag {
    id: string;
    name: string;
    createdAt: string;
}

export interface JournalEntrySystemTag {
    id: string;
    key: string;
    name: string;
    responses?: Record<string, unknown>;
}

export interface SystemTagFieldDefinition {
    id: string;
    fieldKey: string;
    label: string;
    inputType: string;
    required: boolean;
    fieldOrder: number;
    placeholder?: string | null;
    hint?: string | null;
    options?: string[] | null;
    minValue?: number | null;
    maxValue?: number | null;
    stepValue?: number | null;
}

export interface SystemTag {
    id: string;
    key: string;
    name: string;
    description?: string | null;
    fieldDefinitions: SystemTagFieldDefinition[];
}

export interface JournalEntrySystemTagRequest {
    systemTagId: string;
    responses?: Record<string, unknown>;
}

export interface CreateJournalEntryRequest {
    content: string;
    tagIds?: string[];
    systemTagIds?: string[];
    systemTagResponses?: JournalEntrySystemTagRequest[];
}

export interface UpdateJournalEntryRequest {
    content: string;
    tagIds?: string[];
    systemTagIds?: string[];
    systemTagResponses?: JournalEntrySystemTagRequest[];
}

export interface JournalEntryCount {
    count: number;
}

export interface JournalStreakSummary {
    currentStreak: number;
    longestStreak: number;
}

export class JournalService {
    /**
     * Get all journal entries for the current user (paginated)
     */
    static async getAllJournalEntries(options?: {
        page?: number;
        pageSize?: number;
    }): Promise<PagedResponse<JournalEntry>> {
        const params = new URLSearchParams();
        if (options?.page) {
            params.append('page', options.page.toString());
        }
        if (options?.pageSize) {
            params.append('pageSize', options.pageSize.toString());
        }

        const queryParam = params.toString() ? `?${params.toString()}` : '';
        const response = await callBackendApi(`/journal${queryParam}`, {
            method: 'GET',
        });

        if (!response.ok) {
            throw new Error('Failed to fetch journal entries');
        }

        return response.json();
    }

    /**
     * Get a journal entry by ID
     */
    static async getJournalEntry(id: string): Promise<JournalEntry> {
        const response = await callBackendApi(`/journal/${id}`, {
            method: 'GET',
        });

        if (!response.ok) {
            throw new Error('Failed to fetch journal entry');
        }

        return response.json();
    }

    /**
     * Get the count of journal entries for the current user
     */
    static async getJournalEntryCount(): Promise<JournalEntryCount> {
        const response = await callBackendApi('/journal/count', {
            method: 'GET',
        });

        if (!response.ok) {
            throw new Error('Failed to fetch journal entry count');
        }

        return response.json();
    }

    /**
     * Get recent journal entries for the current user
     */
    static async getRecentJournalEntries(
        count: number = 3
    ): Promise<JournalEntry[]> {
        const response = await callBackendApi(
            `/journal/recent?count=${count}`,
            {
                method: 'GET',
            }
        );

        if (!response.ok) {
            throw new Error('Failed to fetch recent journal entries');
        }

        return response.json();
    }

    static async getJournalStreakSummary(): Promise<JournalStreakSummary> {
        const response = await callBackendApi('/journal/streak-summary', {
            method: 'GET',
        });

        if (!response.ok) {
            throw new Error('Failed to fetch journal streak summary');
        }

        return response.json();
    }

    static async getSystemTags(): Promise<SystemTag[]> {
        const response = await callBackendApi('/journal/system-tags', {
            method: 'GET',
        });

        if (!response.ok) {
            throw new Error('Failed to fetch system tags');
        }

        return response.json();
    }

    /**
     * Create a new journal entry
     */
    static async createJournalEntry(
        request: CreateJournalEntryRequest
    ): Promise<JournalEntry> {
        const response = await callBackendApi('/journal', {
            method: 'POST',
            body: request,
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to create journal entry');
        }

        return response.json();
    }

    /**
     * Update a journal entry
     */
    static async updateJournalEntry(
        id: string,
        request: UpdateJournalEntryRequest
    ): Promise<JournalEntry> {
        const response = await callBackendApi(`/journal/${id}`, {
            method: 'PUT',
            body: request,
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to update journal entry');
        }

        return response.json();
    }

    /**
     * Delete a journal entry
     */
    static async deleteJournalEntry(journalEntryId: string): Promise<void> {
        const response = await callBackendApi(`/journal/${journalEntryId}`, {
            method: 'DELETE',
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to delete journal entry');
        }
    }
}
