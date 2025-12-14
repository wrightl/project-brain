import { callBackendApi } from '@/_lib/backend-api';
import { PagedResponse } from '@/_lib/types';

export interface Connection {
    id: string;
    userId: string;
    coachId: string;
    status: 'pending' | 'accepted' | 'cancelled' | 'rejected';
    userName?: string;
    coachName?: string;
    requestedAt: string;
    respondedAt?: string;
}

export class ConnectionService {
    /**
     * Get all connections for the current user (paginated)
     */
    static async getConnections(options?: {
        page?: number;
        pageSize?: number;
    }): Promise<PagedResponse<Connection>> {
        const params = new URLSearchParams();
        if (options?.page) {
            params.append('page', options.page.toString());
        }
        if (options?.pageSize) {
            params.append('pageSize', options.pageSize.toString());
        }

        const queryParam = params.toString() ? `?${params.toString()}` : '';
        const response = await callBackendApi(`/connections${queryParam}`);
        if (!response.ok) {
            throw new Error('Failed to fetch connections');
        }
        return response.json();
    }

    /**
     * Get connection by ID
     */
    static async getConnectionById(connectionId: string): Promise<Connection | null> {
        const response = await callBackendApi(`/connections/${connectionId}`);
        if (response.status === 404) {
            return null;
        }
        if (!response.ok) {
            throw new Error('Failed to fetch connection');
        }
        return response.json();
    }

    /**
     * Cancel or delete a connection
     */
    static async cancelOrDeleteConnection(connectionId: string): Promise<void> {
        const response = await callBackendApi(`/connections/${connectionId}`, {
            method: 'DELETE',
        });
        if (!response.ok) {
            throw new Error('Failed to cancel/delete connection');
        }
    }
}

