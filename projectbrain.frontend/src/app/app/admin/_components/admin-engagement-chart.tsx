'use client';

import { useState, useEffect } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';

interface Point {
    date: string;
    count: number;
}

export function AdminEngagementChart() {
    const [data, setData] = useState<Point[]>([]);
    const [loading, setLoading] = useState(true);
    const [metric, setMetric] = useState<'conversations' | 'quiz-responses'>(
        'conversations',
    );

    useEffect(() => {
        let cancelled = false;
        setLoading(true);
        fetchWithAuth(
            `/api/admin/dashboard/engagement-series?metric=${metric}&days=14`,
        )
            .then((res) => res.json())
            .then((arr: Point[]) => {
                if (!cancelled) setData(Array.isArray(arr) ? arr : []);
            })
            .catch(() => {
                if (!cancelled) setData([]);
            })
            .finally(() => {
                if (!cancelled) setLoading(false);
            });
        return () => {
            cancelled = true;
        };
    }, [metric]);

    const maxCount =
        data.length > 0 ? Math.max(...data.map((d) => d.count), 1) : 1;

    return (
        <div className="rounded-lg border border-gray-300 bg-white p-6 shadow flex flex-col h-full">
            <div className="flex items-center justify-between mb-4 gap-3">
                <h3 className="text-lg font-semibold text-gray-900">
                    Engagement over time
                </h3>
                <select
                    value={metric}
                    onChange={(e) =>
                        setMetric(
                            e.target.value as
                                | 'conversations'
                                | 'quiz-responses',
                        )
                    }
                    className="rounded-md border border-gray-300 bg-white text-gray-900 text-sm px-2 py-1.5 focus:ring-2 focus:ring-[color:var(--indigo)]"
                >
                    <option value="conversations">Conversations</option>
                    <option value="quiz-responses">Quiz responses</option>
                </select>
            </div>
            <p className="text-xs text-gray-500 mb-4">
                Last 14 days · All workspaces
            </p>
            <div className="flex-1 min-h-[200px] flex items-end gap-1">
                {loading ? (
                    <div className="flex-1 flex items-center justify-center text-gray-500">
                        Loading…
                    </div>
                ) : data.length === 0 ? (
                    <div className="flex-1 flex items-center justify-center text-gray-500">
                        No data
                    </div>
                ) : (
                    <div className="flex-1 w-full flex items-end gap-1 h-[220px]">
                        {data.map((point) => (
                            <div
                                key={point.date}
                                className="flex-1 flex flex-col items-center justify-end gap-1 h-full"
                            >
                                <div
                                    className="w-full rounded-t min-h-[6px] bg-[color:var(--indigo)]"
                                    style={{
                                        height: `${Math.max(
                                            6,
                                            (point.count / maxCount) * 100,
                                        )}%`,
                                    }}
                                    title={`${point.date}: ${point.count}`}
                                />
                                <span className="text-[10px] text-gray-500 truncate w-full text-center">
                                    {point.date.slice(5)}
                                </span>
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}
