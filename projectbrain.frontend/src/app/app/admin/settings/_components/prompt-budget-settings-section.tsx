'use client';

import { useEffect, useState } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import toast from 'react-hot-toast';

interface PromptBudgetSettings {
    enablePromptBudget: boolean;
    systemReserve: number;
    policiesReserve: number;
    preferencesReserve: number;
    queryReserve: number;
    summaryReserve: number;
    factsReserve: number;
    episodesReserve: number;
    onboardingReserve: number;
    historyReserve: number;
}

function parseSettings(data: Record<string, unknown>): PromptBudgetSettings {
    return {
        enablePromptBudget: !!(data.enablePromptBudget ?? data.EnablePromptBudget),
        systemReserve: Number(data.systemReserve ?? data.SystemReserve ?? 400),
        policiesReserve: Number(data.policiesReserve ?? data.PoliciesReserve ?? 200),
        preferencesReserve: Number(
            data.preferencesReserve ?? data.PreferencesReserve ?? 150
        ),
        queryReserve: Number(data.queryReserve ?? data.QueryReserve ?? 200),
        summaryReserve: Number(data.summaryReserve ?? data.SummaryReserve ?? 300),
        factsReserve: Number(data.factsReserve ?? data.FactsReserve ?? 250),
        episodesReserve: Number(data.episodesReserve ?? data.EpisodesReserve ?? 200),
        onboardingReserve: Number(
            data.onboardingReserve ?? data.OnboardingReserve ?? 400
        ),
        historyReserve: Number(data.historyReserve ?? data.HistoryReserve ?? 800),
    };
}

const RESERVE_FIELDS: Array<{
    name: keyof Omit<PromptBudgetSettings, 'enablePromptBudget'>;
    label: string;
    description: string;
}> = [
    {
        name: 'systemReserve',
        label: 'System instructions reserve',
        description: 'Tokens reserved for core system instructions',
    },
    {
        name: 'policiesReserve',
        label: 'Policies reserve',
        description: 'Tokens reserved for safety and policy blocks',
    },
    {
        name: 'preferencesReserve',
        label: 'Preferences reserve',
        description: 'Tokens reserved for user preference context',
    },
    {
        name: 'queryReserve',
        label: 'Query reserve',
        description: 'Tokens reserved for the current user message',
    },
    {
        name: 'summaryReserve',
        label: 'Summary reserve',
        description: 'Tokens reserved for conversation summary',
    },
    {
        name: 'factsReserve',
        label: 'Facts reserve',
        description: 'Tokens reserved for retrieved user facts',
    },
    {
        name: 'episodesReserve',
        label: 'Episodes reserve',
        description: 'Tokens reserved for retrieved episodes',
    },
    {
        name: 'onboardingReserve',
        label: 'Onboarding reserve',
        description: 'Tokens reserved for onboarding profile data',
    },
    {
        name: 'historyReserve',
        label: 'History reserve',
        description: 'Tokens reserved for recent chat history',
    },
];

export default function PromptBudgetSettingsSection() {
    const [settings, setSettings] = useState<PromptBudgetSettings>(parseSettings({}));
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
            const response = await fetchWithAuth('/api/admin/settings/prompt-budget');
            if (!response.ok) {
                throw new Error('Failed to load prompt budget settings');
            }
            const data = await response.json();
            setSettings(parseSettings(data));
        } catch (err) {
            const message =
                err instanceof Error
                    ? err.message
                    : 'Failed to load prompt budget settings';
            setError(message);
            toast.error(message);
        } finally {
            setLoading(false);
        }
    };

    const handleNumberChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setSettings((prev) => ({
            ...prev,
            [name]: parseInt(value, 10) || 0,
        }));
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setSaving(true);

        const reserveValues = RESERVE_FIELDS.map((field) => settings[field.name]);
        if (reserveValues.some((value) => value < 50)) {
            setError('Token reserves must be at least 50');
            setSaving(false);
            return;
        }

        try {
            const response = await fetchWithAuth('/api/admin/settings/prompt-budget', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(settings),
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => null);
                throw new Error(
                    (errorData && (errorData.error || errorData.message)) ||
                        'Failed to update prompt budget settings'
                );
            }

            const data = await response.json();
            setSettings(parseSettings(data));
            toast.success('Prompt budget settings updated successfully');
        } catch (err) {
            const message =
                err instanceof Error
                    ? err.message
                    : 'Failed to update prompt budget settings';
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
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="bg-white shadow rounded-lg p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">
                Prompt Budget Settings
            </h2>
            <p className="text-sm text-gray-600 mb-6">
                Reserve token budgets per prompt slot. When enabled, chat assembly
                drops lower-priority content before exceeding the model limit.
            </p>

            <form onSubmit={handleSubmit} className="space-y-6">
                {error && (
                    <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
                        {error}
                    </div>
                )}

                <label className="flex items-center justify-between gap-4">
                    <div>
                        <div className="text-sm font-medium text-gray-900">
                            Enable prompt budget
                        </div>
                        <div className="text-xs text-gray-500">
                            When off, chat uses the legacy truncate-after-build flow.
                        </div>
                    </div>
                    <input
                        type="checkbox"
                        checked={settings.enablePromptBudget}
                        onChange={() =>
                            setSettings((prev) => ({
                                ...prev,
                                enablePromptBudget: !prev.enablePromptBudget,
                            }))
                        }
                        className="h-5 w-5 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                    />
                </label>

                <h3 className="text-base font-semibold text-gray-900 pt-2">
                    Slot reserves (tokens)
                </h3>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    {RESERVE_FIELDS.map((field) => (
                        <div key={field.name}>
                            <label
                                htmlFor={field.name}
                                className="block text-sm font-medium text-gray-700"
                            >
                                {field.label}
                            </label>
                            <input
                                type="number"
                                id={field.name}
                                name={field.name}
                                min="50"
                                required
                                value={settings[field.name]}
                                onChange={handleNumberChange}
                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            />
                            <p className="mt-1 text-xs text-gray-500">
                                {field.description}
                            </p>
                        </div>
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
