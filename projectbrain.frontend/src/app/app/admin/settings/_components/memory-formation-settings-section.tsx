'use client';

import { useEffect, useState } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import toast from 'react-hot-toast';

interface MemoryFormationSettings {
    enableMemoryFormation: boolean;
    minPromotionConfidence: number;
    provisionalConfidence: number;
    activationObservationCount: number;
    maxFactsPerTurn: number;
    maxEpisodesPerTurn: number;
    maxFactsRetrieved: number;
    maxEpisodesRetrieved: number;
    indexProvisionalMemories: boolean;
    enableMemoryDecay: boolean;
    provisionalTtlDays: number;
    activeFactTtlDays: number;
    activeEpisodeTtlDays: number;
    decayInactivityDays: number;
}

function parseSettings(data: Record<string, unknown>): MemoryFormationSettings {
    return {
        enableMemoryFormation: !!(data.enableMemoryFormation ?? data.EnableMemoryFormation),
        minPromotionConfidence: Number(
            data.minPromotionConfidence ?? data.MinPromotionConfidence ?? 0.75
        ),
        provisionalConfidence: Number(
            data.provisionalConfidence ?? data.ProvisionalConfidence ?? 0.6
        ),
        activationObservationCount: Number(
            data.activationObservationCount ?? data.ActivationObservationCount ?? 2
        ),
        maxFactsPerTurn: Number(data.maxFactsPerTurn ?? data.MaxFactsPerTurn ?? 3),
        maxEpisodesPerTurn: Number(
            data.maxEpisodesPerTurn ?? data.MaxEpisodesPerTurn ?? 2
        ),
        maxFactsRetrieved: Number(
            data.maxFactsRetrieved ?? data.MaxFactsRetrieved ?? 5
        ),
        maxEpisodesRetrieved: Number(
            data.maxEpisodesRetrieved ?? data.MaxEpisodesRetrieved ?? 3
        ),
        indexProvisionalMemories: !!(
            data.indexProvisionalMemories ?? data.IndexProvisionalMemories
        ),
        enableMemoryDecay: !!(
            data.enableMemoryDecay ?? data.EnableMemoryDecay ?? true
        ),
        provisionalTtlDays: Number(
            data.provisionalTtlDays ?? data.ProvisionalTtlDays ?? 30
        ),
        activeFactTtlDays: Number(
            data.activeFactTtlDays ?? data.ActiveFactTtlDays ?? 365
        ),
        activeEpisodeTtlDays: Number(
            data.activeEpisodeTtlDays ?? data.ActiveEpisodeTtlDays ?? 180
        ),
        decayInactivityDays: Number(
            data.decayInactivityDays ?? data.DecayInactivityDays ?? 90
        ),
    };
}

export default function MemoryFormationSettingsSection() {
    const [settings, setSettings] = useState<MemoryFormationSettings>({
        enableMemoryFormation: true,
        minPromotionConfidence: 0.75,
        provisionalConfidence: 0.6,
        activationObservationCount: 2,
        maxFactsPerTurn: 3,
        maxEpisodesPerTurn: 2,
        maxFactsRetrieved: 5,
        maxEpisodesRetrieved: 3,
        indexProvisionalMemories: false,
        enableMemoryDecay: true,
        provisionalTtlDays: 30,
        activeFactTtlDays: 365,
        activeEpisodeTtlDays: 180,
        decayInactivityDays: 90,
    });
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
                '/api/admin/settings/memory-formation'
            );
            if (!response.ok) {
                throw new Error('Failed to load memory formation settings');
            }
            const data = await response.json();
            setSettings(parseSettings(data));
        } catch (err) {
            const message =
                err instanceof Error
                    ? err.message
                    : 'Failed to load memory formation settings';
            setError(message);
            toast.error(message);
        } finally {
            setLoading(false);
        }
    };

    const handleNumberChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value, type, step } = e.target;
        setSettings((prev) => ({
            ...prev,
            [name]:
                type === 'number' && step
                    ? parseFloat(value) || 0
                    : parseInt(value, 10) || 0,
        }));
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setSaving(true);

        if (
            settings.minPromotionConfidence < 0 ||
            settings.minPromotionConfidence > 1 ||
            settings.provisionalConfidence < 0 ||
            settings.provisionalConfidence > 1 ||
            settings.activationObservationCount < 1
        ) {
            setError(
                'Confidence values must be between 0 and 1; activation count must be at least 1'
            );
            setSaving(false);
            return;
        }

        try {
            const response = await fetchWithAuth(
                '/api/admin/settings/memory-formation',
                {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(settings),
                }
            );

            if (!response.ok) {
                const errorData = await response.json().catch(() => null);
                throw new Error(
                    (errorData && (errorData.error || errorData.message)) ||
                        'Failed to update memory formation settings'
                );
            }

            const data = await response.json();
            setSettings(parseSettings(data));
            toast.success('Memory formation settings updated successfully');
        } catch (err) {
            const message =
                err instanceof Error
                    ? err.message
                    : 'Failed to update memory formation settings';
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
                Memory Formation Settings
            </h2>
            <p className="text-sm text-gray-600 mb-6">
                Control extraction of user facts and episodes from chat, promotion
                thresholds, and how many memories are retrieved per turn.
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
                            Enable memory formation
                        </div>
                        <div className="text-xs text-gray-500">
                            When off, chat behaves like Phase 1 (no fact/episode
                            extraction or retrieval).
                        </div>
                    </div>
                    <input
                        type="checkbox"
                        checked={settings.enableMemoryFormation}
                        onChange={() =>
                            setSettings((prev) => ({
                                ...prev,
                                enableMemoryFormation: !prev.enableMemoryFormation,
                            }))
                        }
                        className="h-5 w-5 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                    />
                </label>

                <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                    <div>
                        <label
                            htmlFor="minPromotionConfidence"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Min promotion confidence
                        </label>
                        <input
                            type="number"
                            id="minPromotionConfidence"
                            name="minPromotionConfidence"
                            min="0"
                            max="1"
                            step="0.01"
                            required
                            value={settings.minPromotionConfidence}
                            onChange={handleNumberChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                        <p className="mt-1 text-xs text-gray-500">
                            Minimum confidence to promote a candidate to active
                        </p>
                    </div>

                    <div>
                        <label
                            htmlFor="provisionalConfidence"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Provisional confidence
                        </label>
                        <input
                            type="number"
                            id="provisionalConfidence"
                            name="provisionalConfidence"
                            min="0"
                            max="1"
                            step="0.01"
                            required
                            value={settings.provisionalConfidence}
                            onChange={handleNumberChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                        <p className="mt-1 text-xs text-gray-500">
                            Minimum confidence to store as provisional
                        </p>
                    </div>

                    <div>
                        <label
                            htmlFor="activationObservationCount"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Activation observation count
                        </label>
                        <input
                            type="number"
                            id="activationObservationCount"
                            name="activationObservationCount"
                            min="1"
                            required
                            value={settings.activationObservationCount}
                            onChange={handleNumberChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                        <p className="mt-1 text-xs text-gray-500">
                            Observations before provisional memory becomes active
                        </p>
                    </div>

                    <div>
                        <label
                            htmlFor="maxFactsPerTurn"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Max facts per turn
                        </label>
                        <input
                            type="number"
                            id="maxFactsPerTurn"
                            name="maxFactsPerTurn"
                            min="0"
                            required
                            value={settings.maxFactsPerTurn}
                            onChange={handleNumberChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                        <p className="mt-1 text-xs text-gray-500">
                            Cap on fact candidates extracted after each chat turn
                        </p>
                    </div>

                    <div>
                        <label
                            htmlFor="maxEpisodesPerTurn"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Max episodes per turn
                        </label>
                        <input
                            type="number"
                            id="maxEpisodesPerTurn"
                            name="maxEpisodesPerTurn"
                            min="0"
                            required
                            value={settings.maxEpisodesPerTurn}
                            onChange={handleNumberChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                        <p className="mt-1 text-xs text-gray-500">
                            Cap on episode candidates extracted after each turn
                        </p>
                    </div>

                    <div>
                        <label
                            htmlFor="maxFactsRetrieved"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Max facts retrieved
                        </label>
                        <input
                            type="number"
                            id="maxFactsRetrieved"
                            name="maxFactsRetrieved"
                            min="0"
                            required
                            value={settings.maxFactsRetrieved}
                            onChange={handleNumberChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                        <p className="mt-1 text-xs text-gray-500">
                            Facts injected into the prompt per chat turn
                        </p>
                    </div>

                    <div>
                        <label
                            htmlFor="maxEpisodesRetrieved"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Max episodes retrieved
                        </label>
                        <input
                            type="number"
                            id="maxEpisodesRetrieved"
                            name="maxEpisodesRetrieved"
                            min="0"
                            required
                            value={settings.maxEpisodesRetrieved}
                            onChange={handleNumberChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                        <p className="mt-1 text-xs text-gray-500">
                            Episodes injected into the prompt per chat turn
                        </p>
                    </div>
                </div>

                <h3 className="text-base font-semibold text-gray-900 pt-2">
                    Memory lifecycle
                </h3>
                <p className="text-sm text-gray-600 -mt-2">
                    Control automatic expiry of provisional and inactive memories.
                </p>

                <label className="flex items-center justify-between gap-4">
                    <div>
                        <div className="text-sm font-medium text-gray-900">
                            Enable memory decay
                        </div>
                        <div className="text-xs text-gray-500">
                            When off, the daily decay job does nothing.
                        </div>
                    </div>
                    <input
                        type="checkbox"
                        checked={settings.enableMemoryDecay}
                        onChange={() =>
                            setSettings((prev) => ({
                                ...prev,
                                enableMemoryDecay: !prev.enableMemoryDecay,
                            }))
                        }
                        className="h-5 w-5 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                    />
                </label>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                        <label
                            htmlFor="provisionalTtlDays"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Provisional TTL (days)
                        </label>
                        <input
                            type="number"
                            id="provisionalTtlDays"
                            name="provisionalTtlDays"
                            min="0"
                            required
                            value={settings.provisionalTtlDays}
                            onChange={handleNumberChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                    </div>

                    <div>
                        <label
                            htmlFor="decayInactivityDays"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Decay inactivity (days)
                        </label>
                        <input
                            type="number"
                            id="decayInactivityDays"
                            name="decayInactivityDays"
                            min="0"
                            required
                            value={settings.decayInactivityDays}
                            onChange={handleNumberChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                    </div>

                    <div>
                        <label
                            htmlFor="activeFactTtlDays"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Active fact TTL (days)
                        </label>
                        <input
                            type="number"
                            id="activeFactTtlDays"
                            name="activeFactTtlDays"
                            min="0"
                            required
                            value={settings.activeFactTtlDays}
                            onChange={handleNumberChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                    </div>

                    <div>
                        <label
                            htmlFor="activeEpisodeTtlDays"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Active episode TTL (days)
                        </label>
                        <input
                            type="number"
                            id="activeEpisodeTtlDays"
                            name="activeEpisodeTtlDays"
                            min="0"
                            required
                            value={settings.activeEpisodeTtlDays}
                            onChange={handleNumberChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                    </div>
                </div>

                <label className="flex items-center justify-between gap-4">
                    <div>
                        <div className="text-sm font-medium text-gray-900">
                            Index provisional memories
                        </div>
                        <div className="text-xs text-gray-500">
                            Include provisional memories in search indexing (default
                            off; only active memories are retrieved).
                        </div>
                    </div>
                    <input
                        type="checkbox"
                        checked={settings.indexProvisionalMemories}
                        onChange={() =>
                            setSettings((prev) => ({
                                ...prev,
                                indexProvisionalMemories:
                                    !prev.indexProvisionalMemories,
                            }))
                        }
                        className="h-5 w-5 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                    />
                </label>

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
