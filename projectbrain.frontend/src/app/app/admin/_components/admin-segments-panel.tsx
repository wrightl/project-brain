'use client';

import { useState, useEffect } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import type { AdminDashboardAggregateResponse } from '@/_services/admin-dashboard-service';

const SEGMENT_COLORS = [
    { bg: 'bg-emerald-500', label: 'Users' },
    { bg: 'bg-sky-400', label: 'Coaches' },
    { bg: 'bg-amber-500', label: 'Active (logged in)' },
];

export function AdminSegmentsPanel() {
    const [data, setData] = useState<AdminDashboardAggregateResponse | null>(
        null
    );
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        let cancelled = false;
        fetchWithAuth('/api/admin/dashboard/aggregate-usage')
            .then((res) => res.json())
            .then((d: AdminDashboardAggregateResponse) => {
                if (!cancelled) setData(d);
            })
            .catch(() => {
                if (!cancelled) setData(null);
            })
            .finally(() => {
                if (!cancelled) setLoading(false);
            });
        return () => {
            cancelled = true;
        };
    }, []);

    const segments = data
        ? [
            { label: 'Users', value: data.normalUsers, color: SEGMENT_COLORS[0].bg },
            { label: 'Coaches', value: data.totalCoaches, color: SEGMENT_COLORS[1].bg },
            { label: 'Active (logged in)', value: data.loggedInUsers, color: SEGMENT_COLORS[2].bg },
        ]
        : [];

    return (
        <div
            className="rounded-lg p-6 border border-gray-300 shadow flex flex-col"
            style={{ background: 'var(--dashboard-panel-bg)', minWidth: '280px' }}
        >
            <h3 className="text-base font-semibold text-white mb-4">
                Top segments
            </h3>
            {loading ? (
                <p className="text-sm text-gray-500">Loading…</p>
            ) : (
                <ul className="space-y-3">
                    {segments.map((seg) => (
                        <li
                            key={seg.label}
                            className="flex items-center gap-3"
                        >
                            <span
                                className={`w-3 h-3 rounded-full flex-shrink-0 ${seg.color}`}
                            />
                            <span className="text-sm font-medium text-gray-200">
                                {seg.label}
                            </span>
                            <span className="ml-auto text-sm font-semibold text-white">
                                {seg.value.toLocaleString()}
                            </span>
                        </li>
                    ))}
                </ul>
            )}
            {data && !loading && (
                <div className="mt-4 pt-4 border-t border-gray-600 text-xs text-gray-400">
                    <p>AI queries (today): {data.totalAiQueriesDaily}</p>
                    <p>Storage: {data.totalFileStorageMegabytes.toFixed(1)} MB</p>
                </div>
            )}
        </div>
    );
}
