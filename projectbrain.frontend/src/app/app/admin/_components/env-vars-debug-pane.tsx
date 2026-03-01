'use client';

import { useState, useEffect } from 'react';
import { ChevronDownIcon, ChevronRightIcon } from '@heroicons/react/24/outline';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';

interface EnvResponse {
    env: Record<string, string>;
}

export default function EnvVarsDebugPane() {
    const [open, setOpen] = useState(false);
    const [env, setEnv] = useState<Record<string, string> | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (!open || env !== null) return;
        setLoading(true);
        setError(null);
        fetchWithAuth('/api/admin/debug/env')
            .then(async (response) => {
                if (!response.ok) throw new Error('Failed to load env');
                const data = (await response.json()) as EnvResponse;
                setEnv(data.env);
            })
            .catch((e) => {
                setError(e instanceof Error ? e.message : 'Failed to load env');
            })
            .finally(() => {
                setLoading(false);
            });
    }, [open, env]);

    return (
        <div className="rounded-lg border border-gray-300 bg-white shadow">
            <button
                type="button"
                onClick={() => setOpen((o) => !o)}
                className="flex w-full items-center justify-between px-4 py-3 text-left text-sm font-medium text-gray-900 hover:bg-gray-100"
            >
                <span>Frontend environment variables (debug)</span>
                {open ? (
                    <ChevronDownIcon className="h-5 w-5 text-gray-500" />
                ) : (
                    <ChevronRightIcon className="h-5 w-5 text-gray-500" />
                )}
            </button>
            {open && (
                <div className="border-t border-gray-300 px-4 py-3">
                    {loading && (
                        <p className="text-sm text-gray-600">Loading…</p>
                    )}
                    {error && (
                        <p className="text-sm text-red-600">{error}</p>
                    )}
                    {env && !loading && (
                        <div className="overflow-x-auto">
                            <table className="min-w-full text-sm">
                                <thead>
                                    <tr className="border-b border-gray-200 text-left text-gray-600">
                                        <th className="pb-2 pr-4 font-medium">
                                            Variable
                                        </th>
                                        <th className="pb-2 font-medium">
                                            Value
                                        </th>
                                    </tr>
                                </thead>
                                <tbody className="text-gray-900">
                                    {Object.entries(env).map(([key, value]) => (
                                        <tr
                                            key={key}
                                            className="border-b border-gray-100"
                                        >
                                            <td className="py-1.5 pr-4 font-mono text-gray-700">
                                                {key}
                                            </td>
                                            <td className="py-1.5 font-mono break-all">
                                                {value}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}
