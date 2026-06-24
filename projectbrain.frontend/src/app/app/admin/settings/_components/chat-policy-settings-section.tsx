'use client';

import { useEffect, useState } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { toSentenceCase } from '@/_lib/utils';
import toast from 'react-hot-toast';

interface ChatPolicySetting {
    key: string;
    value: string;
    description?: string | null;
}

function policyLabel(key: string): string {
    const prefix = 'AI:Policy:';
    const raw = key.startsWith(prefix) ? key.slice(prefix.length) : key;
    return toSentenceCase(raw);
}

export default function ChatPolicySettingsSection() {
    const [policies, setPolicies] = useState<ChatPolicySetting[]>([]);
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
            const response = await fetchWithAuth('/api/admin/settings/chat-policies');
            if (!response.ok) {
                throw new Error('Failed to load chat policy settings');
            }
            const data = await response.json();
            setPolicies(
                (data.policies ?? []).map((p: ChatPolicySetting) => ({
                    key: p.key,
                    value: p.value ?? '',
                    description: p.description ?? null,
                }))
            );
        } catch (err) {
            const message =
                err instanceof Error
                    ? err.message
                    : 'Failed to load chat policy settings';
            setError(message);
            console.error('Error loading chat policy settings:', err);
            toast.error(message);
        } finally {
            setLoading(false);
        }
    };

    const handlePolicyChange = (index: number, value: string) => {
        setPolicies((prev) =>
            prev.map((policy, i) =>
                i === index ? { ...policy, value } : policy
            )
        );
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setSaving(true);

        if (policies.some((p) => !p.value.trim())) {
            setError('All policy values must be non-empty');
            setSaving(false);
            return;
        }

        try {
            const response = await fetchWithAuth('/api/admin/settings/chat-policies', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ policies }),
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => null);
                throw new Error(
                    (errorData && (errorData.error || errorData.message)) ||
                        'Failed to update chat policy settings'
                );
            }

            const data = await response.json();
            setPolicies(
                (data.policies ?? []).map((p: ChatPolicySetting) => ({
                    key: p.key,
                    value: p.value ?? '',
                    description: p.description ?? null,
                }))
            );
            toast.success('Chat policy settings updated successfully');
        } catch (err) {
            const message =
                err instanceof Error
                    ? err.message
                    : 'Failed to update chat policy settings';
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
                        <div className="h-20 bg-gray-200 rounded"></div>
                        <div className="h-20 bg-gray-200 rounded"></div>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="bg-white shadow rounded-lg p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">
                Chat Policy Settings
            </h2>
            <p className="text-sm text-gray-600 mb-6">
                Guardrails injected into every chat turn (crisis guidance, tone,
                and citation rules).
            </p>

            <form onSubmit={handleSubmit} className="space-y-6">
                {error && (
                    <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
                        {error}
                    </div>
                )}

                {policies.length === 0 ? (
                    <p className="text-sm text-gray-600">
                        No chat policies found. Ensure application settings are
                        seeded.
                    </p>
                ) : (
                    policies.map((policy, index) => (
                        <div key={policy.key}>
                            <label
                                htmlFor={`policy-${index}`}
                                className="block text-sm font-medium text-gray-700"
                            >
                                {policyLabel(policy.key)}
                            </label>
                            {policy.description && (
                                <p className="mt-1 text-xs text-gray-500">
                                    {policy.description}
                                </p>
                            )}
                            <textarea
                                id={`policy-${index}`}
                                rows={4}
                                required
                                value={policy.value}
                                onChange={(e) =>
                                    handlePolicyChange(index, e.target.value)
                                }
                                className="mt-2 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            />
                        </div>
                    ))
                )}

                <div className="flex justify-end space-x-3 pt-4 border-t border-gray-200">
                    <button
                        type="submit"
                        disabled={saving || policies.length === 0}
                        className="px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 disabled:bg-gray-400"
                    >
                        {saving ? 'Saving...' : 'Save Changes'}
                    </button>
                </div>
            </form>
        </div>
    );
}
