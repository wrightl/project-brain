'use client';

import { useEffect, useState } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import type { CommunityReport } from '@/_services/community-service';

export default function AdminCommunityPage() {
    const [reports, setReports] = useState<CommunityReport[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const load = async () => {
        try {
            setLoading(true);
            const response = await fetchWithAuth(
                '/api/community/admin/reports',
            );
            if (!response.ok) {
                throw new Error('Failed to load reports');
            }
            setReports((await response.json()) as CommunityReport[]);
            setError(null);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to load');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        load();
    }, []);

    const hidePost = async (postId: string, reportId: string) => {
        const hideResponse = await fetchWithAuth(
            `/api/community/posts/${postId}/hide`,
            { method: 'POST' },
        );
        if (!hideResponse.ok) return;
        await fetchWithAuth(`/api/community/admin/reports/${reportId}/resolve`, {
            method: 'POST',
        });
        await load();
    };

    const resolveOnly = async (reportId: string) => {
        await fetchWithAuth(`/api/community/admin/reports/${reportId}/resolve`, {
            method: 'POST',
        });
        await load();
    };

    return (
        <div className="space-y-6">
            <div>
                <h1 className="text-2xl font-semibold text-gray-900">
                    Community moderation
                </h1>
                <p className="mt-2 text-gray-600">
                    Review open reports. Enable or disable the Community hub in
                    Settings → Feature flags.
                </p>
            </div>

            {loading && <p className="text-gray-600">Loading…</p>}
            {error && <p className="text-red-600">{error}</p>}
            {!loading && reports.length === 0 && (
                <p className="text-gray-600">No open reports.</p>
            )}

            <ul className="space-y-4">
                {reports.map((report) => (
                    <li
                        key={report.id}
                        className="rounded-lg border border-gray-300 bg-white p-4"
                    >
                        <p className="text-sm text-gray-500">
                            #{report.channelSlug} · reported by{' '}
                            {report.reporterDisplayName}
                        </p>
                        <p className="mt-2 text-gray-900">
                            {report.postBodyPreview}
                        </p>
                        <p className="mt-2 text-sm text-gray-700">
                            Reason: {report.reason}
                        </p>
                        <div className="mt-3 flex gap-3 text-sm">
                            <button
                                type="button"
                                onClick={() =>
                                    hidePost(report.postId, report.id)
                                }
                                className="text-red-600 hover:text-red-700"
                            >
                                Hide post
                            </button>
                            <button
                                type="button"
                                onClick={() => resolveOnly(report.id)}
                                className="text-gray-700 hover:text-gray-900"
                            >
                                Dismiss report
                            </button>
                        </div>
                    </li>
                ))}
            </ul>
        </div>
    );
}
