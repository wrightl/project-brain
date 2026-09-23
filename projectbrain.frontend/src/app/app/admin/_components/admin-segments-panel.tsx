'use client';

import { useState, useEffect } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import type { AdminDashboardAggregateResponse } from '@/_services/admin-dashboard-service';

const SEGMENT_DOT_COLORS = [
    'bg-[color:var(--indigo)]',
    'bg-[color:var(--aqua)]',
    'bg-[color:var(--emerald)]',
];

export function AdminSegmentsPanel() {
    const [data, setData] = useState<AdminDashboardAggregateResponse | null>(
        null,
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
              {
                  label: 'Users',
                  value: data.normalUsers,
                  color: SEGMENT_DOT_COLORS[0],
              },
              {
                  label: 'Coaches',
                  value: data.totalCoaches,
                  color: SEGMENT_DOT_COLORS[1],
              },
              {
                  label: 'Active (logged in)',
                  value: data.loggedInUsers,
                  color: SEGMENT_DOT_COLORS[2],
              },
          ]
        : [];

    return (
        <div className="rounded-lg border border-gray-300 bg-white p-6 shadow flex flex-col min-w-0 lg:min-w-[280px]">
            <h3 className="text-base font-semibold text-gray-900 mb-4">
                Top segments
            </h3>
            {loading ? (
                <p className="text-sm text-gray-500">Loading…</p>
            ) : (
                <ul className="space-y-3">
                    {segments.map((seg) => (
                        <li key={seg.label} className="flex items-center gap-3">
                            <span
                                className={`w-3 h-3 rounded-full flex-shrink-0 ${seg.color}`}
                            />
                            <span className="text-sm font-medium text-gray-700">
                                {seg.label}
                            </span>
                            <span className="ml-auto text-sm font-semibold text-gray-900">
                                {seg.value.toLocaleString()}
                            </span>
                        </li>
                    ))}
                </ul>
            )}
            {data && !loading && (
                <div className="mt-4 pt-4 border-t border-gray-300 text-xs text-gray-500 space-y-1">
                    <p>AI queries (today): {data.totalAiQueriesDaily}</p>
                    <p>
                        Storage: {data.totalFileStorageMegabytes.toFixed(1)} MB
                    </p>
                </div>
            )}
        </div>
    );
}
