import { callBackendApi } from '@/_lib/backend-api';

export interface UserFactMemory {
    id: string;
    content: string;
    category: string;
    status: string;
    createdAt: string;
}

export interface UserEpisodeMemory {
    id: string;
    summary: string;
    topic: string;
    outcome: string;
    status: string;
    createdAt: string;
}

export interface UserMemoryList {
    facts: UserFactMemory[];
    episodes: UserEpisodeMemory[];
}

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
}
