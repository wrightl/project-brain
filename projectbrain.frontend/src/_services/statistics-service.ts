import { callBackendApi } from '@/_lib/backend-api';

export type TimePeriod =
    | '24h'
    | 'last24hours'
    | '3d'
    | 'last3days'
    | '7d'
    | 'last7days'
    | '30d'
    | 'last30days'
    | 'thismonth'
    | 'lastmonth';

interface StatisticResponse {
    count: number;
    period?: string;
}

export class StatisticsService {
    /**
     * Get count of conversations for logged in user (optionally filtered by time period)
     */
    static async getUserConversations(period?: TimePeriod): Promise<number> {
        const queryParam = period ? `?period=${period}` : '';
        const response = await callBackendApi(
            `/statistics/user-conversations${queryParam}`
        );
        if (!response.ok) {
            throw new Error('Failed to fetch user conversations count');
        }
        const data: StatisticResponse = await response.json();
        return data.count;
    }

    /**
     * Get count of resources for logged in user
     */
    static async getUserResources(): Promise<number> {
        const response = await callBackendApi('/statistics/user-resources');
        if (!response.ok) {
            throw new Error('Failed to fetch user resources count');
        }
        const data: StatisticResponse = await response.json();
        return data.count;
    }

    /**
     * Get count of clients a coach is connected to (accepted)
     */
    static async getCoachClients(): Promise<number> {
        const response = await callBackendApi('/statistics/coach-clients');
        if (!response.ok) {
            throw new Error('Failed to fetch coach clients count');
        }
        const data: StatisticResponse = await response.json();
        return data.count;
    }

    /**
     * Get count of clients a coach is pending connection to (pending)
     */
    static async getPendingClients(): Promise<number> {
        const response = await callBackendApi(
            '/statistics/coach-clients-pending'
        );
        if (!response.ok) {
            throw new Error('Failed to fetch pending clients count');
        }
        const data: StatisticResponse = await response.json();
        return data.count;
    }

    /**
     * Get count of shared resources
     */
    static async getSharedResources(): Promise<number> {
        const response = await callBackendApi('/statistics/shared-resources');
        if (!response.ok) {
            throw new Error('Failed to fetch shared resources count');
        }
        const data: StatisticResponse = await response.json();
        return data.count;
    }

    /**
     * Get count of all users
     */
    static async getAllUsers(): Promise<number> {
        const response = await callBackendApi('/statistics/all-users');
        if (!response.ok) {
            throw new Error('Failed to fetch all users count');
        }
        const data: StatisticResponse = await response.json();
        return data.count;
    }

    /**
     * Get count of coaches
     */
    static async getCoaches(): Promise<number> {
        const response = await callBackendApi('/statistics/coaches');
        if (!response.ok) {
            throw new Error('Failed to fetch coaches count');
        }
        const data: StatisticResponse = await response.json();
        return data.count;
    }

    /**
     * Get count of normal users
     */
    static async getNormalUsers(): Promise<number> {
        const response = await callBackendApi('/statistics/normal-users');
        if (!response.ok) {
            throw new Error('Failed to fetch normal users count');
        }
        const data: StatisticResponse = await response.json();
        return data.count;
    }

    /**
     * Get count of quizzes
     */
    static async getQuizzes(): Promise<number> {
        const response = await callBackendApi('/statistics/quizzes');
        if (!response.ok) {
            throw new Error('Failed to fetch quizzes count');
        }
        const data: StatisticResponse = await response.json();
        return data.count;
    }

    /**
     * Get count of quiz responses for a time period
     */
    static async getQuizResponses(period?: TimePeriod): Promise<number> {
        const queryParam = period ? `?period=${period}` : '';
        const response = await callBackendApi(
            `/statistics/quiz-responses${queryParam}`
        );
        if (!response.ok) {
            throw new Error('Failed to fetch quiz responses count');
        }
        const data: StatisticResponse = await response.json();
        return data.count;
    }

    /**
     * Get count of logged in users
     */
    static async getLoggedInUsers(): Promise<number> {
        const response = await callBackendApi('/statistics/logged-in-users');
        if (!response.ok) {
            throw new Error('Failed to fetch logged in users count');
        }
        const data: StatisticResponse = await response.json();
        return data.count;
    }

    /**
     * Get count of conversations for a time period
     */
    static async getConversations(period?: TimePeriod): Promise<number> {
        const queryParam = period ? `?period=${period}` : '';
        const response = await callBackendApi(
            `/statistics/conversations${queryParam}`
        );
        if (!response.ok) {
            throw new Error('Failed to fetch conversations count');
        }
        const data: StatisticResponse = await response.json();
        return data.count;
    }

    /**
     * Get count of all conversations for a time period
     */
    static async getAllConversations(period?: TimePeriod): Promise<number> {
        const queryParam = period ? `?period=${period}` : '';
        const response = await callBackendApi(
            `/statistics/all-conversations${queryParam}`
        );
        if (!response.ok) {
            throw new Error('Failed to fetch conversations count');
        }
        const data: StatisticResponse = await response.json();
        return data.count;
    }
}
