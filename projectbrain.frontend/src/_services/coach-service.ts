import { callBackendApi } from '@/_lib/backend-api';
import { Coach, User } from '@/_lib/types';

export interface ClientWithConnectionStatus {
    user: User;
    connectionStatus: 'pending' | 'accepted';
    requestedAt: string;
    requestedBy: 'user' | 'coach';
    message?: string;
}

export interface CoachSearchParams {
    city?: string;
    stateProvince?: string;
    country?: string;
    ageGroups?: string[];
    specialisms?: string[];
}

export class CoachService {
    /**
     * Search for coaches based on location, age groups, and specialisms
     */
    static async searchCoaches(params: CoachSearchParams): Promise<Coach[]> {
        const queryParams = new URLSearchParams();

        if (params.city) {
            queryParams.append('city', params.city);
        }
        if (params.stateProvince) {
            queryParams.append('stateProvince', params.stateProvince);
        }
        if (params.country) {
            queryParams.append('country', params.country);
        }
        if (params.ageGroups && params.ageGroups.length > 0) {
            params.ageGroups.forEach((ag) => {
                queryParams.append('ageGroups', ag);
            });
        }
        if (params.specialisms && params.specialisms.length > 0) {
            params.specialisms.forEach((s) => {
                queryParams.append('specialisms', s);
            });
        }

        const queryString = queryParams.toString();
        const endpoint = `/coaches/search${
            queryString ? `?${queryString}` : ''
        }`;

        const response = await callBackendApi(endpoint);
        if (!response.ok) {
            throw new Error('Failed to search coaches');
        }
        return response.json();
    }

    /**
     * Get coach by user ID
     */
    static async getCoachById(userId: string): Promise<Coach | null> {
        const response = await callBackendApi(`/coaches/${userId}`);
        if (response.status === 404) {
            return null;
        }
        if (!response.ok) {
            throw new Error('Failed to fetch coach');
        }
        return response.json();
    }

    /**
     * Get all connected clients/users for the current coach
     */
    static async getConnectedClients(): Promise<ClientWithConnectionStatus[]> {
        const response = await callBackendApi('/coaches/clients');
        if (!response.ok) {
            throw new Error('Failed to fetch connected clients');
        }
        const data = await response.json();
        // Transform the response to match our frontend interface
        // Backend returns { User, ConnectionStatus, RequestedAt, RequestedBy, Message }
        return data.map((item: any) => ({
            user: item.User || item.user,
            connectionStatus:
                (item.ConnectionStatus || item.connectionStatus) === 'accepted'
                    ? 'accepted'
                    : 'pending',
            requestedAt: item.RequestedAt || item.requestedAt,
            requestedBy: item.RequestedBy || item.requestedBy,
            message: item.Message || item.message,
        }));
    }

    /**
     * Accept a connection request from a user
     */
    static async acceptClientConnection(userId: string): Promise<void> {
        const response = await callBackendApi(
            `/coaches/clients/${userId}/accept`,
            {
                method: 'POST',
            }
        );
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(
                errorData.error?.message || 'Failed to accept connection'
            );
        }
    }
}
