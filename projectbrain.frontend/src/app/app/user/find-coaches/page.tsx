'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import {
    MagnifyingGlassIcon,
    MapPinIcon,
    UserGroupIcon,
    AcademicCapIcon,
} from '@heroicons/react/24/outline';
import { CoachSearchParams } from '@/_lib/types';
import { Coach } from '@/_lib/types';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import AvailabilityBadge from '@/_components/coach/availability-badge';

interface ConnectionStatus {
    status: 'none' | 'pending' | 'connected';
    connectionId?: string;
    requestedAt?: string;
    respondedAt?: string;
    requestedBy?: 'user' | 'coach';
}

export default function FindCoachesPage() {
    const router = useRouter();
    const [searchParams, setSearchParams] = useState<CoachSearchParams>({
        city: '',
        stateProvince: '',
        country: '',
        ageGroups: [],
        specialisms: [],
    });
    const [coaches, setCoaches] = useState<Coach[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [hasSearched, setHasSearched] = useState(false);
    const [connectionStatuses, setConnectionStatuses] = useState<
        Record<string, ConnectionStatus>
    >({});
    const [connectingCoaches, setConnectingCoaches] = useState<Set<string>>(
        new Set()
    );

    // Common age groups and specialisms (you may want to make these dynamic)
    const commonAgeGroups = [
        'Children (5-12)',
        'Teens (13-17)',
        'Young Adults (18-25)',
        'Adults (26-40)',
        'Middle-aged (41-60)',
        'Seniors (60+)',
    ];

    const commonSpecialisms = [
        'ADHD',
        'Autism',
        'Dyslexia',
        'Anxiety',
        'Depression',
        'Executive Functioning',
        'Social Skills',
        'Learning Disabilities',
        'Behavioral Issues',
        'Career Coaching',
        'Life Coaching',
        'Academic Support',
    ];

    const handleSearch = async (e: React.FormEvent) => {
        e.preventDefault();
        setLoading(true);
        setError(null);
        setHasSearched(true);

        try {
            const queryParams = new URLSearchParams();

            if (searchParams.city) {
                queryParams.append('city', searchParams.city);
            }
            if (searchParams.stateProvince) {
                queryParams.append('stateProvince', searchParams.stateProvince);
            }
            if (searchParams.country) {
                queryParams.append('country', searchParams.country);
            }
            if (searchParams.ageGroups && searchParams.ageGroups.length > 0) {
                searchParams.ageGroups.forEach((ag) => {
                    queryParams.append('ageGroups', ag);
                });
            }
            if (
                searchParams.specialisms &&
                searchParams.specialisms.length > 0
            ) {
                searchParams.specialisms.forEach((s) => {
                    queryParams.append('specialisms', s);
                });
            }

            const queryString = queryParams.toString();
            const response = await fetchWithAuth(
                `/api/coaches/search${queryString ? `?${queryString}` : ''}`
            );

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(
                    errorData.error ||
                        `Failed to search coaches: ${response.status}`
                );
            }

            const results: Coach[] = await response.json();
            setCoaches(results);

            // Fetch connection status for all coaches
            const statusPromises = results.map(async (coach) => {
                try {
                    const statusResponse = await fetchWithAuth(
                        `/api/coaches/${coach.coachProfileId}/connection-status`
                    );
                    if (statusResponse.ok) {
                        const status: ConnectionStatus =
                            await statusResponse.json();
                        return { coachId: coach.coachProfileId, status };
                    }
                } catch (err) {
                    console.error(
                        `Error fetching connection status for coach ${coach.coachProfileId}:`,
                        err
                    );
                }
                return {
                    coachId: coach.coachProfileId,
                    status: { status: 'none' as const },
                };
            });

            const statusResults = await Promise.all(statusPromises);
            const statusMap: Record<string, ConnectionStatus> = {};
            statusResults.forEach(({ coachId, status }) => {
                statusMap[coachId] = status;
            });
            setConnectionStatuses(statusMap);
        } catch (err) {
            setError(
                err instanceof Error ? err.message : 'Failed to search coaches'
            );
            setCoaches([]);
        } finally {
            setLoading(false);
        }
    };

    const handleContactCoach = async (coach: Coach) => {
        const connectionStatus = connectionStatuses[coach.coachProfileId];
        if (connectionStatus?.connectionId) {
            router.push(`/app/user/messages/${connectionStatus.connectionId}`);
        } else {
            // Fallback: if connection ID is not available, try to get it
            console.error(
                'Connection ID not available for coach:',
                coach.coachProfileId
            );
        }
    };

    const handleConnectCoach = async (coach: Coach) => {
        if (connectingCoaches.has(coach.coachProfileId)) return;

        setConnectingCoaches((prev) => new Set(prev).add(coach.coachProfileId));
        try {
            const response = await fetchWithAuth(
                `/api/coaches/${coach.coachProfileId}/connections`,
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                }
            );

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(
                    errorData.error?.message ||
                        'Failed to send connection request'
                );
            }

            // Update connection status to pending
            setConnectionStatuses((prev) => ({
                ...prev,
                [coach.coachProfileId]: { status: 'pending' },
            }));
        } catch (err) {
            setError(
                err instanceof Error
                    ? err.message
                    : 'Failed to send connection request'
            );
        } finally {
            setConnectingCoaches((prev) => {
                const newSet = new Set(prev);
                newSet.delete(coach.coachProfileId);
                return newSet;
            });
        }
    };

    // Helper function to check if coach is online and available
    const isCoachOnlineAndAvailable = (coach: Coach): boolean => {
        if (coach.availabilityStatus !== 'Available') {
            return false;
        }

        // Check if coach was active in the last 30 minutes
        if (coach.lastActivityAt) {
            const lastActivity = new Date(coach.lastActivityAt);
            const now = new Date();
            const minutesSinceActivity =
                (now.getTime() - lastActivity.getTime()) / (1000 * 60);
            return minutesSinceActivity <= 30;
        }

        return false;
    };

    const toggleAgeGroup = (ageGroup: string) => {
        setSearchParams((prev) => ({
            ...prev,
            ageGroups: prev.ageGroups?.includes(ageGroup)
                ? prev.ageGroups.filter((ag) => ag !== ageGroup)
                : [...(prev.ageGroups || []), ageGroup],
        }));
    };

    const toggleSpecialism = (specialism: string) => {
        setSearchParams((prev) => ({
            ...prev,
            specialisms: prev.specialisms?.includes(specialism)
                ? prev.specialisms.filter((s) => s !== specialism)
                : [...(prev.specialisms || []), specialism],
        }));
    };

    return (
        <div className="space-y-6">
            <div>
                <h1 className="text-2xl font-bold text-gray-900">
                    Find a Coach
                </h1>
                <p className="mt-1 text-sm text-gray-600">
                    Search for coaches based on location, age groups, and
                    specialisms
                </p>
            </div>

            {/* Search Form */}
            <form
                onSubmit={handleSearch}
                className="bg-white shadow rounded-lg p-6"
            >
                <div className="space-y-6">
                    {/* Location Fields */}
                    <div>
                        <h3 className="text-sm font-medium text-gray-900 mb-4">
                            Location
                        </h3>
                        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                            <div>
                                <label
                                    htmlFor="city"
                                    className="block text-sm font-medium text-gray-700"
                                >
                                    City
                                </label>
                                <div className="mt-1 relative">
                                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                        <MapPinIcon className="h-5 w-5 text-gray-400" />
                                    </div>
                                    <input
                                        type="text"
                                        id="city"
                                        value={searchParams.city || ''}
                                        onChange={(e) =>
                                            setSearchParams((prev) => ({
                                                ...prev,
                                                city: e.target.value,
                                            }))
                                        }
                                        placeholder="Enter city"
                                        className="block w-full pl-10 rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                    />
                                </div>
                            </div>
                            <div>
                                <label
                                    htmlFor="stateProvince"
                                    className="block text-sm font-medium text-gray-700"
                                >
                                    State/Province
                                </label>
                                <input
                                    type="text"
                                    id="stateProvince"
                                    value={searchParams.stateProvince || ''}
                                    onChange={(e) =>
                                        setSearchParams((prev) => ({
                                            ...prev,
                                            stateProvince: e.target.value,
                                        }))
                                    }
                                    placeholder="Enter state/province"
                                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                />
                            </div>
                            <div>
                                <label
                                    htmlFor="country"
                                    className="block text-sm font-medium text-gray-700"
                                >
                                    Country
                                </label>
                                <input
                                    type="text"
                                    id="country"
                                    value={searchParams.country || ''}
                                    onChange={(e) =>
                                        setSearchParams((prev) => ({
                                            ...prev,
                                            country: e.target.value,
                                        }))
                                    }
                                    placeholder="Enter country"
                                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                />
                            </div>
                        </div>
                    </div>

                    {/* Age Groups */}
                    <div>
                        <h3 className="text-sm font-medium text-gray-900 mb-4 flex items-center">
                            <UserGroupIcon className="h-5 w-5 mr-2 text-gray-400" />
                            Age Groups
                        </h3>
                        <div className="flex flex-wrap gap-2">
                            {commonAgeGroups.map((ageGroup) => (
                                <button
                                    key={ageGroup}
                                    type="button"
                                    onClick={() => toggleAgeGroup(ageGroup)}
                                    className={`px-3 py-1 rounded-full text-sm font-medium transition-colors ${
                                        searchParams.ageGroups?.includes(
                                            ageGroup
                                        )
                                            ? 'bg-indigo-600 text-white'
                                            : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                                    }`}
                                >
                                    {ageGroup}
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Specialisms */}
                    <div>
                        <h3 className="text-sm font-medium text-gray-900 mb-4 flex items-center">
                            <AcademicCapIcon className="h-5 w-5 mr-2 text-gray-400" />
                            Specialisms
                        </h3>
                        <div className="flex flex-wrap gap-2">
                            {commonSpecialisms.map((specialism) => (
                                <button
                                    key={specialism}
                                    type="button"
                                    onClick={() => toggleSpecialism(specialism)}
                                    className={`px-3 py-1 rounded-full text-sm font-medium transition-colors ${
                                        searchParams.specialisms?.includes(
                                            specialism
                                        )
                                            ? 'bg-indigo-600 text-white'
                                            : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                                    }`}
                                >
                                    {specialism}
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Search Button */}
                    <div>
                        <button
                            type="submit"
                            disabled={loading}
                            className="inline-flex items-center px-4 py-2 bg-indigo-600 text-white font-medium rounded-md hover:bg-indigo-700 transition-colors disabled:bg-gray-300 disabled:cursor-not-allowed"
                        >
                            <MagnifyingGlassIcon className="h-5 w-5 mr-2" />
                            {loading ? 'Searching...' : 'Search Coaches'}
                        </button>
                    </div>
                </div>
            </form>

            {/* Error Message */}
            {error && (
                <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                    <p className="text-sm text-red-800">{error}</p>
                </div>
            )}

            {/* Results */}
            <div className="bg-white shadow rounded-lg p-6">
                <h2 className="text-lg font-medium text-gray-900 mb-4">
                    Search Results ({coaches.length})
                </h2>
                {!hasSearched ? (
                    <div className="text-center py-12 text-gray-500">
                        <MagnifyingGlassIcon className="mx-auto h-12 w-12 text-gray-400" />
                        <p className="mt-2">
                            Enter search criteria and click &quot;Search
                            Coaches&quot; to find coaches
                        </p>
                    </div>
                ) : coaches.length === 0 ? (
                    <div className="text-center py-12 text-gray-500">
                        <MagnifyingGlassIcon className="mx-auto h-12 w-12 text-gray-400" />
                        <p className="mt-2">
                            No coaches found matching your criteria
                        </p>
                    </div>
                ) : (
                    <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
                        {coaches.map((coach: Coach) => (
                            <div
                                key={coach.coachProfileId}
                                className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow"
                            >
                                <div className="flex items-start justify-between">
                                    <div className="flex-1">
                                        <div className="flex items-center gap-2">
                                            <h3 className="text-lg font-semibold text-gray-900">
                                                {coach.fullName}
                                            </h3>
                                            {coach.availabilityStatus && (
                                                <AvailabilityBadge
                                                    status={
                                                        coach.availabilityStatus
                                                    }
                                                    size="sm"
                                                />
                                            )}
                                        </div>
                                        {coach.city && (
                                            <p className="text-sm text-gray-600 mt-1 flex items-center">
                                                <MapPinIcon className="h-4 w-4 mr-1" />
                                                {[
                                                    coach.city,
                                                    coach.stateProvince,
                                                    coach.country,
                                                ]
                                                    .filter(Boolean)
                                                    .join(', ')}
                                            </p>
                                        )}
                                    </div>
                                </div>

                                {coach.specialisms.length > 0 && (
                                    <div className="mt-3">
                                        <p className="text-xs font-medium text-gray-500 mb-1">
                                            Specialisms:
                                        </p>
                                        <div className="flex flex-wrap gap-1">
                                            {coach.specialisms
                                                .slice(0, 3)
                                                .map((s) => (
                                                    <span
                                                        key={s}
                                                        className="px-2 py-0.5 bg-indigo-100 text-indigo-800 text-xs rounded"
                                                    >
                                                        {s}
                                                    </span>
                                                ))}
                                            {coach.specialisms.length > 3 && (
                                                <span className="px-2 py-0.5 text-xs text-gray-500">
                                                    +
                                                    {coach.specialisms.length -
                                                        3}{' '}
                                                    more
                                                </span>
                                            )}
                                        </div>
                                    </div>
                                )}

                                {coach.ageGroups.length > 0 && (
                                    <div className="mt-2">
                                        <p className="text-xs font-medium text-gray-500 mb-1">
                                            Age Groups:
                                        </p>
                                        <div className="flex flex-wrap gap-1">
                                            {coach.ageGroups
                                                .slice(0, 2)
                                                .map((ag) => (
                                                    <span
                                                        key={ag}
                                                        className="px-2 py-0.5 bg-green-100 text-green-800 text-xs rounded"
                                                    >
                                                        {ag}
                                                    </span>
                                                ))}
                                            {coach.ageGroups.length > 2 && (
                                                <span className="px-2 py-0.5 text-xs text-gray-500">
                                                    +
                                                    {coach.ageGroups.length - 2}{' '}
                                                    more
                                                </span>
                                            )}
                                        </div>
                                    </div>
                                )}

                                <div className="mt-4 flex gap-2">
                                    <button
                                        onClick={() =>
                                            router.push(
                                                `/app/user/coaches/${coach.coachProfileId}`
                                            )
                                        }
                                        className="flex-1 px-3 py-2 text-sm font-medium text-indigo-600 bg-indigo-50 rounded-md hover:bg-indigo-100 transition-colors"
                                    >
                                        View Details
                                    </button>
                                    {(() => {
                                        const connectionStatus =
                                            connectionStatuses[
                                                coach.coachProfileId
                                            ]?.status || 'none';
                                        const isConnected =
                                            connectionStatus === 'connected';
                                        const isPending =
                                            connectionStatus === 'pending';
                                        const canContact =
                                            isConnected &&
                                            isCoachOnlineAndAvailable(coach);

                                        if (isConnected) {
                                            return (
                                                <button
                                                    onClick={() =>
                                                        handleContactCoach(
                                                            coach as Coach
                                                        )
                                                    }
                                                    disabled={!canContact}
                                                    className="flex-1 px-3 py-2 text-sm font-medium text-white bg-indigo-600 rounded-md hover:bg-indigo-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                                                >
                                                    Contact
                                                </button>
                                            );
                                        } else if (isPending) {
                                            return (
                                                <button
                                                    disabled
                                                    className="flex-1 px-3 py-2 text-sm font-medium text-gray-600 bg-gray-200 rounded-md cursor-not-allowed"
                                                >
                                                    Pending
                                                </button>
                                            );
                                        } else {
                                            return (
                                                <button
                                                    onClick={() =>
                                                        handleConnectCoach(
                                                            coach as Coach
                                                        )
                                                    }
                                                    disabled={connectingCoaches.has(
                                                        coach.coachProfileId
                                                    )}
                                                    className="flex-1 px-3 py-2 text-sm font-medium text-white bg-indigo-600 rounded-md hover:bg-indigo-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                                                >
                                                    {connectingCoaches.has(
                                                        coach.coachProfileId
                                                    )
                                                        ? 'Connecting...'
                                                        : 'Connect'}
                                                </button>
                                            );
                                        }
                                    })()}
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}
