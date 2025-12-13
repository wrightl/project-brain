'use client';

import { useState, useEffect, useCallback } from 'react';
import {
    UsersIcon,
    CloudArrowUpIcon,
    ChartBarIcon,
    DocumentTextIcon,
} from '@heroicons/react/24/outline';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';

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

interface AdminStatisticsProps {
    initialStats: {
        allUsers: number;
        coaches: number;
        sharedResources: number;
        quizzes: number;
        normalUsers: number;
        loggedInUsers: number;
    };
}

export default function AdminStatistics({
    initialStats,
}: AdminStatisticsProps) {
    const [period, setPeriod] = useState<TimePeriod>('7d');
    const [conversationsCount, setConversationsCount] = useState<number>(0);
    const [quizResponsesCount, setQuizResponsesCount] = useState<number>(0);
    const [loading, setLoading] = useState(false);

    const periodOptions: { value: TimePeriod; label: string }[] = [
        { value: '24h', label: 'Last 24 Hours' },
        { value: '3d', label: 'Last 3 Days' },
        { value: '7d', label: 'Last 7 Days' },
        { value: '30d', label: 'Last 30 Days' },
        { value: 'thismonth', label: 'This Month' },
        { value: 'lastmonth', label: 'Last Month' },
    ];

    const fetchPeriodStats = useCallback(async (selectedPeriod: TimePeriod) => {
        setLoading(true);
        try {
            const queryParam = selectedPeriod
                ? `?period=${selectedPeriod}`
                : '';
            const [conversationsResponse, quizResponsesResponse] =
                await Promise.all([
                    fetchWithAuth(
                        `/api/admin/statistics/conversations${queryParam}`
                    ),
                    fetchWithAuth(
                        `/api/admin/statistics/quiz-responses${queryParam}`
                    ),
                ]);

            if (!conversationsResponse.ok || !quizResponsesResponse.ok) {
                throw new Error('Failed to fetch statistics');
            }

            const conversationsData: StatisticResponse =
                await conversationsResponse.json();
            const quizResponsesData: StatisticResponse =
                await quizResponsesResponse.json();

            setConversationsCount(conversationsData.count);
            setQuizResponsesCount(quizResponsesData.count);
        } catch (error) {
            console.error('Error fetching period statistics:', error);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        fetchPeriodStats(period);
    }, [period, fetchPeriodStats]);

    const stats = [
        {
            name: 'Total Users',
            value: initialStats.allUsers.toString(),
            icon: UsersIcon,
        },
        {
            name: 'Total Coaches',
            value: initialStats.coaches.toString(),
            icon: UsersIcon,
        },
        {
            name: 'Normal Users',
            value: initialStats.normalUsers.toString(),
            icon: UsersIcon,
        },
        {
            name: 'Logged In Users',
            value: initialStats.loggedInUsers.toString(),
            icon: UsersIcon,
        },
        {
            name: 'Knowledge Files',
            value: initialStats.sharedResources.toString(),
            icon: CloudArrowUpIcon,
        },
        {
            name: `Conversations (${
                periodOptions.find((p) => p.value === period)?.label || period
            })`,
            value: loading ? '...' : conversationsCount.toString(),
            icon: ChartBarIcon,
        },
        {
            name: 'Total Quizzes',
            value: initialStats.quizzes.toString(),
            icon: DocumentTextIcon,
        },
        {
            name: `Quiz Responses (${
                periodOptions.find((p) => p.value === period)?.label || period
            })`,
            value: loading ? '...' : quizResponsesCount.toString(),
            icon: DocumentTextIcon,
        },
    ];

    return (
        <div className="space-y-6">
            {/* Period Selector */}
            <div className="flex items-center justify-between">
                <h2 className="text-xl font-semibold text-gray-900">
                    Statistics
                </h2>
                <div className="flex items-center gap-2">
                    <label
                        htmlFor="period-select"
                        className="text-sm font-medium text-gray-700"
                    >
                        Time Period:
                    </label>
                    <select
                        id="period-select"
                        value={period}
                        onChange={(e) =>
                            setPeriod(e.target.value as TimePeriod)
                        }
                        className="block rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        disabled={loading}
                    >
                        {periodOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                                {option.label}
                            </option>
                        ))}
                    </select>
                </div>
            </div>

            {/* Stats Grid */}
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
                {stats.map((stat) => {
                    const Icon = stat.icon;
                    return (
                        <div
                            key={stat.name}
                            className="bg-white overflow-hidden shadow rounded-lg"
                        >
                            <div className="p-5">
                                <div className="flex items-center">
                                    <div className="flex-shrink-0">
                                        <Icon
                                            className="h-6 w-6 text-gray-400"
                                            aria-hidden="true"
                                        />
                                    </div>
                                    <div className="ml-5 w-0 flex-1">
                                        <dl>
                                            <dt className="text-sm font-medium text-gray-500 truncate">
                                                {stat.name}
                                            </dt>
                                            <dd className="text-lg font-semibold text-gray-900">
                                                {stat.value}
                                            </dd>
                                        </dl>
                                    </div>
                                </div>
                            </div>
                        </div>
                    );
                })}
            </div>
        </div>
    );
}
