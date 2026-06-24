'use client';

import { clearFlagsCache } from '@/_lib/flags';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { useEffect, useState } from 'react';
import toast from 'react-hot-toast';

interface FeatureFlagItem {
    key: string;
    label: string;
    description: string;
    enabled: boolean;
}

function parseFlagItem(data: Record<string, unknown>): FeatureFlagItem {
    return {
        key: String(data.key ?? data.Key ?? ''),
        label: String(data.label ?? data.Label ?? ''),
        description: String(data.description ?? data.Description ?? ''),
        enabled: !!(data.enabled ?? data.Enabled),
    };
}

export default function FeatureFlagsSettingsSection() {
    const [flags, setFlags] = useState<FeatureFlagItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        loadSettings();
    }, []);

    const loadSettings = async () => {
        try {
            setLoading(true);
            setError(null);
            const response = await fetchWithAuth(
                '/api/admin/settings/feature-flags',
            );
            if (!response.ok) {
                throw new Error('Failed to load feature flag settings');
            }
            const data = await response.json();
            const rawFlags = (data.flags ?? data.Flags ?? []) as Record<
                string,
                unknown
            >[];
            setFlags(rawFlags.map(parseFlagItem));
        } catch (err) {
            const message =
                err instanceof Error
                    ? err.message
                    : 'Failed to load feature flag settings';
            setError(message);
            console.error('Error loading feature flag settings:', err);
            toast.error(message);
        } finally {
            setLoading(false);
        }
    };

    const handleToggle = (key: string) => {
        setFlags((prev) =>
            prev.map((flag) =>
                flag.key === key ? { ...flag, enabled: !flag.enabled } : flag,
            ),
        );
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setSaving(true);

        try {
            const payload = {
                flags: Object.fromEntries(
                    flags.map((flag) => [flag.key, flag.enabled]),
                ),
            };

            const response = await fetchWithAuth(
                '/api/admin/settings/feature-flags',
                {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(payload),
                },
            );

            if (!response.ok) {
                const errorData = await response.json().catch(() => null);
                throw new Error(
                    (errorData && (errorData.error || errorData.message)) ||
                        'Failed to update feature flag settings',
                );
            }

            clearFlagsCache();
            toast.success('Feature flag settings updated successfully');
        } catch (err) {
            const message =
                err instanceof Error
                    ? err.message
                    : 'Failed to update feature flag settings';
            setError(message);
            toast.error(message);
        } finally {
            setSaving(false);
        }
    };

    if (loading) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <div className="animate-pulse">
                    <div className="h-6 bg-gray-200 rounded w-1/3 mb-4"></div>
                    <div className="space-y-4">
                        <div className="h-4 bg-gray-200 rounded"></div>
                        <div className="h-4 bg-gray-200 rounded"></div>
                        <div className="h-4 bg-gray-200 rounded"></div>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="bg-white shadow rounded-lg p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">
                Feature Flags
            </h2>
            <p className="text-sm text-gray-600 mb-6">
                Toggle application feature flags. Changes take effect
                immediately after saving.
            </p>

            <form onSubmit={handleSubmit} className="space-y-6">
                {error && (
                    <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
                        {error}
                    </div>
                )}

                <div className="space-y-4">
                    {flags.map((flag) => (
                        <label
                            key={flag.key}
                            className="flex items-center justify-between gap-4"
                        >
                            <div>
                                <div className="text-sm font-medium text-gray-900">
                                    {flag.label}
                                </div>
                                <div className="text-xs text-gray-500">
                                    {flag.description}
                                </div>
                            </div>
                            <input
                                type="checkbox"
                                checked={flag.enabled}
                                onChange={() => handleToggle(flag.key)}
                                className="h-5 w-5 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                            />
                        </label>
                    ))}
                </div>

                <div className="flex justify-end space-x-3 pt-4 border-t border-gray-200">
                    <button
                        type="submit"
                        disabled={saving}
                        className="px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 disabled:bg-gray-400"
                    >
                        {saving ? 'Saving...' : 'Save Changes'}
                    </button>
                </div>
            </form>
        </div>
    );
}
