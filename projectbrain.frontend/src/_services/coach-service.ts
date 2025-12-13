import { callBackendApi } from '@/_lib/backend-api';
import { Coach, User } from '@/_lib/types';
import { convertAvailabilityStatus } from '@/_lib/availability-status';

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
     * Helper function to transform a coach's availabilityStatus from integer to string
     */
    private static transformCoach(coach: any): Coach {
        return {
            ...coach,
            availabilityStatus: convertAvailabilityStatus(
                coach.availabilityStatus
            ),
        };
    }

    /**
     * Helper function to transform an array of coaches
     */
    private static transformCoaches(coaches: any[]): Coach[] {
        return coaches.map((coach) => this.transformCoach(coach));
    }

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
        const coaches = await response.json();
        return this.transformCoaches(coaches);
    }

    /**
     * Get coach by user ID
     */
    static async getCoachById(id: number): Promise<Coach | null> {
        const response = await callBackendApi(`/coaches/${id}`);
        if (response.status === 404) {
            return null;
        }
        if (!response.ok) {
            throw new Error('Failed to fetch coach');
        }
        const coach = await response.json();
        return this.transformCoach(coach);
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

    /**
     * Get the current coach's availability status
     */
    static async getAvailabilityStatus(): Promise<
        'Available' | 'Busy' | 'Away' | 'Offline'
    > {
        const response = await callBackendApi('/coaches/availability/status');
        if (!response.ok) {
            throw new Error('Failed to get availability status');
        }
        const data = await response.json();
        return convertAvailabilityStatus(data.status);
    }

    /**
     * Set the current coach's availability status
     */
    static async setAvailabilityStatus(
        status: 'Available' | 'Busy' | 'Away' | 'Offline'
    ): Promise<void> {
        const response = await callBackendApi('/coaches/availability/status', {
            method: 'PUT',
            body: { status },
        });
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(
                errorData.error?.message || 'Failed to set availability status'
            );
        }
    }
}
