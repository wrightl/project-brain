import { callBackendApi } from '@/_lib/backend-api';
import { PagedResponse } from '@/_lib/types';

export interface JournalEntry {
    id: string;
    userId: string;
    content: string;
    summary: string | null;
    createdAt: string;
    updatedAt: string;
    tags?: JournalTag[];
}

export interface JournalTag {
    id: string;
    name: string;
    createdAt: string;
}

export interface CreateJournalEntryRequest {
    content: string;
    tagIds?: string[];
}

export interface UpdateJournalEntryRequest {
    content: string;
    tagIds?: string[];
}

export interface JournalEntryCount {
    count: number;
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

export { PagedResponse };
