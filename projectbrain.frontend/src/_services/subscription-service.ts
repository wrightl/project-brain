import { callBackendApi } from '@/_lib/backend-api';

export interface Subscription {
    id?: string;
    tier: string;
    status: string;
    trialEndsAt?: string;
    currentPeriodStart?: string;
    currentPeriodEnd?: string;
    canceledAt?: string;
    userType: string;
}

export interface Usage {
    aiQueries: {
        daily: number;
        monthly: number;
    };
    coachMessages: {
        monthly: number;
    };
    clientMessages: {
        monthly: number;
    };
    fileStorage: {
        bytes: number;
        megabytes: number;
    };
    researchReports: {
        monthly: number;
    };
}

export class SubscriptionService {
    /**
     * Get current subscription for logged in user/coach
     */
    static async getMySubscription(): Promise<Subscription> {
        const response = await callBackendApi('/subscriptions/me');
        if (!response.ok) {
            throw new Error('Failed to fetch subscription');
        }
        return await response.json();
    }

    /**
     * Get current tier for logged in user/coach
     */
    static async getTier(): Promise<{ tier: string; userType: string }> {
        const response = await callBackendApi('/subscriptions/tier');
        if (!response.ok) {
            throw new Error('Failed to fetch tier');
        }
        return await response.json();
    }

    /**
     * Get usage statistics for logged in user/coach
     */
    static async getUsage(): Promise<Usage> {
        const response = await callBackendApi('/subscriptions/usage');
        if (!response.ok) {
            throw new Error('Failed to fetch usage');
        }
        return await response.json();
    }

    /**
     * Create checkout session for subscription upgrade
     */
    static async createCheckout(tier: string, isAnnual: boolean): Promise<{ url: string }> {
        const response = await callBackendApi('/subscriptions/checkout', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ tier, isAnnual }),
        });
        if (!response.ok) {
            throw new Error('Failed to create checkout session');
        }
        return await response.json();
    }

    /**
     * Cancel current subscription
     */
    static async cancelSubscription(): Promise<{ message: string }> {
        const response = await callBackendApi('/subscriptions/cancel', {
            method: 'POST',
        });
        if (!response.ok) {
            throw new Error('Failed to cancel subscription');
        }
        return await response.json();
    }

    /**
     * Start free trial for Pro tier
     */
    static async startTrial(tier: string): Promise<{ message: string }> {
        const response = await callBackendApi('/subscriptions/trial', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ tier }),
        });
        if (!response.ok) {
            throw new Error('Failed to start trial');
        }
        return await response.json();
    }
}

