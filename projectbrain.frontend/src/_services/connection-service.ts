import { callBackendApi } from '@/_lib/backend-api';

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
     * Get all connections for the current user
     */
    static async getConnections(): Promise<Connection[]> {
        const response = await callBackendApi('/connections');
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

