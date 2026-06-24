import { callBackendApi } from '@/_lib/backend-api';
import type {
    UserEpisodeMemory,
    UserFactMemory,
    UserMemoryList,
} from '@/_services/user-memory-types';

export type { UserEpisodeMemory, UserFactMemory, UserMemoryList };

export class UserMemoryService {
    static async listMemories(): Promise<UserMemoryList> {
        const response = await callBackendApi('/user/memory');
        if (!response.ok) {
            throw new Error('Failed to fetch learned memories');
        }
        return response.json();
    }

    static async deleteFact(id: string): Promise<void> {
        const response = await callBackendApi(`/user/memory/facts/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) {
            throw new Error('Failed to delete memory');
        }
    }

    static async deleteEpisode(id: string): Promise<void> {
        const response = await callBackendApi(`/user/memory/episodes/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) {
            throw new Error('Failed to delete memory');
        }
    }

    static async pinFact(id: string): Promise<void> {
        const response = await callBackendApi(`/user/memory/facts/${id}/pin`, {
            method: 'POST',
        });
        if (!response.ok) {
            throw new Error('Failed to pin memory');
        }
    }

    static async unpinFact(id: string): Promise<void> {
        const response = await callBackendApi(`/user/memory/facts/${id}/unpin`, {
            method: 'POST',
        });
        if (!response.ok) {
            throw new Error('Failed to unpin memory');
        }
    }

    static async pinEpisode(id: string): Promise<void> {
        const response = await callBackendApi(`/user/memory/episodes/${id}/pin`, {
            method: 'POST',
        });
        if (!response.ok) {
            throw new Error('Failed to pin memory');
        }
    }

    static async unpinEpisode(id: string): Promise<void> {
        const response = await callBackendApi(`/user/memory/episodes/${id}/unpin`, {
            method: 'POST',
        });
        if (!response.ok) {
            throw new Error('Failed to unpin memory');
        }
    }
}
