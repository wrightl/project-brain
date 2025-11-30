import { callBackendApi } from '@/_lib/backend-api';

export interface RevenueDataPoint {
    date: string;
    amount: number;
}

export class SubscriptionAnalyticsService {
    /**
     * Get paid subscribers count
     */
    static async getPaidSubscribers(userType?: string, startDate?: string, endDate?: string): Promise<Record<string, number>> {
        const params = new URLSearchParams();
        if (userType) params.append('userType', userType);
        if (startDate) params.append('startDate', startDate);
        if (endDate) params.append('endDate', endDate);
        
        const response = await callBackendApi(`/admin/subscriptions/analytics/paid-subscribers?${params.toString()}`);
        if (!response.ok) {
            throw new Error('Failed to fetch paid subscribers count');
        }
        return await response.json();
    }

    /**
     * Get cancelled subscriptions count
     */
    static async getCancelledSubscriptions(userType?: string, startDate?: string, endDate?: string): Promise<Record<string, number>> {
        const params = new URLSearchParams();
        if (userType) params.append('userType', userType);
        if (startDate) params.append('startDate', startDate);
        if (endDate) params.append('endDate', endDate);
        
        const response = await callBackendApi(`/admin/subscriptions/analytics/cancelled?${params.toString()}`);
        if (!response.ok) {
            throw new Error('Failed to fetch cancelled subscriptions count');
        }
        return await response.json();
    }

    /**
     * Get expired subscriptions count
     */
    static async getExpiredSubscriptions(userType?: string, startDate?: string, endDate?: string): Promise<Record<string, number>> {
        const params = new URLSearchParams();
        if (userType) params.append('userType', userType);
        if (startDate) params.append('startDate', startDate);
        if (endDate) params.append('endDate', endDate);
        
        const response = await callBackendApi(`/admin/subscriptions/analytics/expired?${params.toString()}`);
        if (!response.ok) {
            throw new Error('Failed to fetch expired subscriptions count');
        }
        return await response.json();
    }

    /**
     * Get revenue for a time period
     */
    static async getRevenue(userType: string, startDate: string, endDate: string): Promise<{ revenue: number; userType: string; startDate: string; endDate: string }> {
        const params = new URLSearchParams({
            userType,
            startDate,
            endDate,
        });
        
        const response = await callBackendApi(`/admin/subscriptions/analytics/revenue?${params.toString()}`);
        if (!response.ok) {
            throw new Error('Failed to fetch revenue');
        }
        return await response.json();
    }

    /**
     * Get revenue history
     */
    static async getRevenueHistory(userType: string, months: number = 12): Promise<RevenueDataPoint[]> {
        const params = new URLSearchParams({
            userType,
            months: months.toString(),
        });
        
        const response = await callBackendApi(`/admin/subscriptions/analytics/revenue/history?${params.toString()}`);
        if (!response.ok) {
            throw new Error('Failed to fetch revenue history');
        }
        return await response.json();
    }

    /**
     * Get predicted revenue
     */
    static async getPredictedRevenue(userType: string, months: number = 6): Promise<RevenueDataPoint[]> {
        const params = new URLSearchParams({
            userType,
            months: months.toString(),
        });
        
        const response = await callBackendApi(`/admin/subscriptions/analytics/revenue/predicted?${params.toString()}`);
        if (!response.ok) {
            throw new Error('Failed to fetch predicted revenue');
        }
        return await response.json();
    }

    /**
     * Get subscriptions by tier
     */
    static async getSubscriptionsByTier(userType: string): Promise<Record<string, number>> {
        const params = new URLSearchParams({ userType });
        
        const response = await callBackendApi(`/admin/subscriptions/analytics/by-tier?${params.toString()}`);
        if (!response.ok) {
            throw new Error('Failed to fetch subscriptions by tier');
        }
        return await response.json();
    }
}

