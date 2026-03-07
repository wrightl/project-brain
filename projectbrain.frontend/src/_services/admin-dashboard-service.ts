import { callBackendApi } from '@/_lib/backend-api';

export interface EngagementSeriesPoint {
    date: string;
    count: number;
}

export interface AdminDashboardAggregateResponse {
    totalUsers: number;
    totalCoaches: number;
    normalUsers: number;
    loggedInUsers: number;
    totalAiQueriesDaily: number;
    totalAiQueriesMonthly: number;
    totalFileStorageBytes: number;
    totalFileStorageMegabytes: number;
}

export class AdminDashboardService {
    static async getEngagementSeries(
        metric: 'conversations' | 'quiz-responses' = 'conversations',
        days: number = 14
    ): Promise<EngagementSeriesPoint[]> {
        const params = new URLSearchParams({ metric, days: String(days) });
        const response = await callBackendApi(
            `/admin/dashboard/engagement-series?${params.toString()}`
        );
        if (!response.ok) {
            throw new Error('Failed to fetch engagement series');
        }
        const data = await response.json();
        if (!Array.isArray(data)) return [];
        return data.map(
            (p: { Date?: string; date?: string; Count?: number; count?: number }) => ({
                date: p.date ?? p.Date ?? '',
                count: p.count ?? p.Count ?? 0,
            })
        );
    }

    static async getAggregateUsage(): Promise<AdminDashboardAggregateResponse> {
        const response = await callBackendApi(
            '/admin/dashboard/aggregate-usage'
        );
        if (!response.ok) {
            throw new Error('Failed to fetch aggregate usage');
        }
        const data = (await response.json()) as Record<string, unknown>;
        const get = (k: string) =>
            (data[k] as number) ?? (data[k.charAt(0).toUpperCase() + k.slice(1)] as number) ?? 0;
        return {
            totalUsers: get('totalUsers'),
            totalCoaches: get('totalCoaches'),
            normalUsers: get('normalUsers'),
            loggedInUsers: get('loggedInUsers'),
            totalAiQueriesDaily: get('totalAiQueriesDaily'),
            totalAiQueriesMonthly: get('totalAiQueriesMonthly'),
            totalFileStorageBytes: get('totalFileStorageBytes'),
            totalFileStorageMegabytes: Number(get('totalFileStorageMegabytes')) || 0,
        };
    }
}
