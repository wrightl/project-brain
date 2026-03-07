'use client';

import { useState, useEffect, useCallback } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { AdminKpiCard } from './admin-kpi-card';

export type TimePeriod =
    | '24h'
    | '3d'
    | '7d'
    | '30d'
    | 'thismonth'
    | 'lastmonth';

const PERIOD_OPTIONS: { value: TimePeriod; label: string }[] = [
    { value: '24h', label: 'Last 24 Hours' },
    { value: '3d', label: 'Last 3 Days' },
    { value: '7d', label: 'Last 7 Days' },
    { value: '30d', label: 'Last 30 Days' },
    { value: 'thismonth', label: 'This Month' },
    { value: 'lastmonth', label: 'Last Month' },
];

interface AdminKpiRowProps {
    totalUsers: number;
}

export function AdminKpiRow({ totalUsers }: AdminKpiRowProps) {
    const [period, setPeriod] = useState<TimePeriod>('7d');
    const [conversationsCount, setConversationsCount] = useState<number>(0);
    const [quizResponsesCount, setQuizResponsesCount] = useState<number>(0);
    const [loading, setLoading] = useState(false);

    const fetchPeriodStats = useCallback(async (p: TimePeriod) => {
        setLoading(true);
        try {
            const q = `?period=${p}`;
            const [convRes, quizRes] = await Promise.all([
                fetchWithAuth(`/api/admin/statistics/conversations${q}`),
                fetchWithAuth(`/api/admin/statistics/quiz-responses${q}`),
            ]);
            if (convRes.ok && quizRes.ok) {
                const conv = await convRes.json();
                const quiz = await quizRes.json();
                setConversationsCount(conv.count ?? 0);
                setQuizResponsesCount(quiz.count ?? 0);
            }
        } catch {
            // keep previous values
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        fetchPeriodStats(period);
    }, [period, fetchPeriodStats]);

    const periodLabel = PERIOD_OPTIONS.find((o) => o.value === period)?.label ?? period;

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-end gap-2">
                <label
                    htmlFor="admin-period"
                    className="text-sm font-medium text-gray-500"
                >
                    Period:
                </label>
                <select
                    id="admin-period"
                    value={period}
                    onChange={(e) => setPeriod(e.target.value as TimePeriod)}
                    disabled={loading}
                    className="rounded border border-gray-300 bg-white text-gray-900 shadow-sm focus:ring-2 focus:ring-indigo-500 text-sm py-1 px-2"
                >
                    {PERIOD_OPTIONS.map((opt) => (
                        <option key={opt.value} value={opt.value}>
                            {opt.label}
                        </option>
                    ))}
                </select>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-6">
                <AdminKpiCard
                    label="Total users"
                    value={totalUsers.toLocaleString()}
                />
                <AdminKpiCard
                    label={`Conversations (${periodLabel})`}
                    value={loading ? '…' : conversationsCount.toLocaleString()}
                />
                <AdminKpiCard
                    label={`Quiz responses (${periodLabel})`}
                    value={loading ? '…' : quizResponsesCount.toLocaleString()}
                />
            </div>
        </div>
    );
}
