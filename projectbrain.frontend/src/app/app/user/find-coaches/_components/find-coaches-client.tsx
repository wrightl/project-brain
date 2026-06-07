'use client';

import { useEffect, useMemo, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import {
    MagnifyingGlassIcon,
    MapPinIcon,
    UserGroupIcon,
    AcademicCapIcon,
} from '@heroicons/react/24/outline';
import { CheckIcon } from '@heroicons/react/24/solid';
import { Coach, CoachSearchParams, SubscriptionUserType } from '@/_lib/types';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import AvailabilityBadge from '@/_components/coach/availability-badge';
import StarRating from '@/_components/coach/star-rating';
import { CountryCombobox } from '@/_components/location/country-combobox';
import { CityCombobox } from '@/_components/location/city-combobox';
import type { CityOption, CountryOption } from '@/_lib/location-types';
import { CoachResultsMap } from '@/_components/maps/coach-results-map';

interface ConnectionStatus {
    status: 'none' | 'pending' | 'connected';
    connectionId?: string;
    requestedAt?: string;
    respondedAt?: string;
    requestedBy?: SubscriptionUserType;
}

const FIND_COACHES_SEARCH_STATE_KEY = 'projectbrain.findCoachesSearchState.v1';
const FIND_COACHES_SEARCH_STATE_TTL_MS = 1000 * 60 * 60; // 1 hour

function getFilterChipClassName(isSelected: boolean): string {
    const base =
        'inline-flex items-center gap-1.5 px-4 py-2 rounded-full text-sm font-medium border transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 focus-visible:ring-offset-2';
    return isSelected
        ? `${base} border-indigo-600 bg-indigo-600 text-white hover:bg-indigo-700 shadow-sm`
        : `${base} border-gray-300 bg-gray-50 text-gray-700 hover:border-indigo-400 hover:bg-indigo-50 hover:text-indigo-600`;
}

type PersistedFindCoachesState = {
    savedAt: number;
    searchParams: CoachSearchParams;
    selectedCountry: CountryOption | null;
    selectedCity: CityOption | null;
    useMyLocation: boolean;
    distanceMiles: number;
    searchCenter: { latitude: number; longitude: number } | null;
    coaches: Coach[];
    connectionStatuses: Record<string, ConnectionStatus>;
    highlightedCoachId: string | null;
};

export default function FindCoachesClient({
    defaultCountryName,
    userLatitude,
    userLongitude,
}: {
    defaultCountryName: string;
    userLatitude?: number;
    userLongitude?: number;
}) {
    const router = useRouter();
    const urlSearchParams = useSearchParams();

    const [searchParams, setSearchParams] = useState<CoachSearchParams>({
        country: defaultCountryName || '',
        city: '',
        stateProvince: '',
        ageGroups: [],
        specialisms: [],
    });

    const [selectedCountry, setSelectedCountry] =
        useState<CountryOption | null>(null);
    const [selectedCity, setSelectedCity] = useState<CityOption | null>(null);

    const hasUserLocation = useMemo(() => {
        return (
            typeof userLatitude === 'number' &&
            Number.isFinite(userLatitude) &&
            typeof userLongitude === 'number' &&
            Number.isFinite(userLongitude)
        );
    }, [userLatitude, userLongitude]);

    const [useMyLocation, setUseMyLocation] = useState(false);
    const [distanceMiles, setDistanceMiles] = useState<number>(25);
    const [searchCenter, setSearchCenter] = useState<{
        latitude: number;
        longitude: number;
    } | null>(null);

    const [coaches, setCoaches] = useState<Coach[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [hasSearched, setHasSearched] = useState(false);
    const [highlightedCoachId, setHighlightedCoachId] = useState<string | null>(
        null,
    );
    const [connectionStatuses, setConnectionStatuses] = useState<
        Record<string, ConnectionStatus>
    >({});
    const [connectingCoaches, setConnectingCoaches] = useState<Set<string>>(
        new Set(),
    );
    const [resultsView, setResultsView] = useState<'list' | 'map'>('list');

    const persistSearchState = (
        override?: Partial<PersistedFindCoachesState>,
    ) => {
        try {
            const state: PersistedFindCoachesState = {
                savedAt: Date.now(),
                searchParams,
                selectedCountry,
                selectedCity,
                useMyLocation,
                distanceMiles,
                searchCenter,
                coaches,
                connectionStatuses,
                highlightedCoachId,
                ...override,
            };
            sessionStorage.setItem(
                FIND_COACHES_SEARCH_STATE_KEY,
                JSON.stringify(state),
            );
        } catch {
            // ignore storage failures
        }
    };

    useEffect(() => {
        const restore = urlSearchParams.get('restore') === '1';
        if (!restore) return;

        try {
            const raw = sessionStorage.getItem(FIND_COACHES_SEARCH_STATE_KEY);
            if (!raw) return;
            const parsed = JSON.parse(raw) as PersistedFindCoachesState;
            if (!parsed?.savedAt) return;
            if (
                Date.now() - parsed.savedAt >
                FIND_COACHES_SEARCH_STATE_TTL_MS
            ) {
                sessionStorage.removeItem(FIND_COACHES_SEARCH_STATE_KEY);
                return;
            }

            setSearchParams(parsed.searchParams);
            setSelectedCountry(parsed.selectedCountry);
            setSelectedCity(parsed.selectedCity);
            setUseMyLocation(parsed.useMyLocation && hasUserLocation);
            setDistanceMiles(parsed.distanceMiles);
            setSearchCenter(
                parsed.useMyLocation && hasUserLocation
                    ? {
                          latitude: userLatitude as number,
                          longitude: userLongitude as number,
                      }
                    : parsed.searchCenter,
            );
            setCoaches(parsed.coaches);
            setConnectionStatuses(parsed.connectionStatuses);
            setHasSearched(true);
            setResultsView('list');
            setHighlightedCoachId(parsed.highlightedCoachId);

            if (parsed.highlightedCoachId) {
                window.setTimeout(() => {
                    const el = document.getElementById(
                        `coach-${parsed.highlightedCoachId}`,
                    );
                    el?.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }, 0);
            }

            // Clean up URL so refresh doesn't keep restoring forever
            router.replace('/app/user/find-coaches');
        } catch {
            // ignore parse failures
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [hasUserLocation, router, urlSearchParams]);

    useEffect(() => {
        if (!useMyLocation) return;
        if (!hasUserLocation) return;
        setSearchCenter({
            latitude: userLatitude as number,
            longitude: userLongitude as number,
        });
    }, [hasUserLocation, useMyLocation, userLatitude, userLongitude]);

    useEffect(() => {
        if (useMyLocation) return;
        if (selectedCity) {
            setSearchCenter({
                latitude: selectedCity.latitude,
                longitude: selectedCity.longitude,
            });
        } else {
            setSearchCenter(null);
        }
    }, [selectedCity, useMyLocation]);

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
        setHighlightedCoachId(null);

        try {
            const queryParams = new URLSearchParams();

            // Geo search mode: either My Location, or a chosen city center + distance.
            if (useMyLocation) {
                queryParams.append('useMyLocation', 'true');
                queryParams.append('distanceMiles', String(distanceMiles));
            } else if (searchCenter && Number.isFinite(distanceMiles)) {
                queryParams.append('latitude', String(searchCenter.latitude));
                queryParams.append('longitude', String(searchCenter.longitude));
                queryParams.append('distanceMiles', String(distanceMiles));
            } else {
                // Fallback: string-based location search.
                if (searchParams.country) {
                    queryParams.append('country', searchParams.country);
                }
                if (searchParams.city) {
                    queryParams.append('city', searchParams.city);
                }
                if (searchParams.stateProvince) {
                    queryParams.append(
                        'stateProvince',
                        searchParams.stateProvince,
                    );
                }
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
                `/api/coaches/search${queryString ? `?${queryString}` : ''}`,
            );

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(
                    errorData.error ||
                        `Failed to search coaches: ${response.status}`,
                );
            }

            const results: Coach[] = await response.json();
            setCoaches(results);

            // Fetch connection status for all coaches
            const statusPromises = results.map(async (coach) => {
                try {
                    const statusResponse = await fetchWithAuth(
                        `/api/coaches/${coach.coachProfileId}/connection-status`,
                    );
                    if (statusResponse.ok) {
                        const status: ConnectionStatus =
                            await statusResponse.json();
                        return { coachId: coach.coachProfileId, status };
                    }
                } catch (err) {
                    console.error(
                        `Error fetching connection status for coach ${coach.coachProfileId}:`,
                        err,
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

            persistSearchState({
                coaches: results,
                connectionStatuses: statusMap,
            });
        } catch (err) {
            setError(
                err instanceof Error ? err.message : 'Failed to search coaches',
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
                coach.coachProfileId,
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
                },
            );

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(
                    errorData.error?.message ||
                        'Failed to send connection request',
                );
            }

            const data = await response.json();
            const status =
                data.status === 'connected' ? 'connected' : 'pending';
            setConnectionStatuses((prev) => ({
                ...prev,
                [coach.coachProfileId]: { status },
            }));
        } catch (err) {
            setError(
                err instanceof Error
                    ? err.message
                    : 'Failed to send connection request',
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

    const handleCountryChange = (country: CountryOption | null) => {
        if (useMyLocation) return;
        setSelectedCountry(country);
        setSelectedCity(null);
        setSearchCenter(null);
        setSearchParams((prev) => ({
            ...prev,
            country: country?.name || '',
            city: '',
        }));
    };

    const handleCityChange = (city: CityOption | null) => {
        if (useMyLocation) return;
        setSelectedCity(city);
        setSearchCenter(
            city
                ? { latitude: city.latitude, longitude: city.longitude }
                : null,
        );
        setSearchParams((prev) => ({
            ...prev,
            city: city?.city || '',
            stateProvince: city?.stateProvince || prev.stateProvince,
        }));
    };

    const coachesWithCoords = coaches.filter(
        (c) =>
            typeof c.latitude === 'number' &&
            Number.isFinite(c.latitude) &&
            typeof c.longitude === 'number' &&
            Number.isFinite(c.longitude),
    );

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

                        <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between mb-4">
                            <div className="min-h-[40px] flex items-center">
                                {hasUserLocation && (
                                    <div className="flex items-center gap-2">
                                        <input
                                            id="useMyLocation"
                                            type="checkbox"
                                            checked={useMyLocation}
                                            onChange={(e) => {
                                                const next = e.target.checked;
                                                setUseMyLocation(next);
                                                if (next && hasUserLocation) {
                                                    setDistanceMiles((d) =>
                                                        Number.isFinite(d)
                                                            ? d
                                                            : 25,
                                                    );
                                                }
                                            }}
                                            className="h-4 w-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                                        />
                                        <label
                                            htmlFor="useMyLocation"
                                            className="text-sm text-gray-700"
                                        >
                                            My Location
                                        </label>
                                    </div>
                                )}
                            </div>

                            <div className="sm:w-56">
                                <label
                                    htmlFor="distanceMiles"
                                    className="block text-sm font-medium text-gray-700"
                                >
                                    Distance
                                </label>
                                <select
                                    id="distanceMiles"
                                    value={distanceMiles}
                                    disabled={!searchCenter}
                                    onChange={(e) =>
                                        setDistanceMiles(
                                            Number(e.target.value) || 25,
                                        )
                                    }
                                    className={`mt-1 block w-full rounded-md border-gray-300 bg-white shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm ${
                                        !searchCenter
                                            ? 'bg-gray-50 cursor-not-allowed'
                                            : ''
                                    }`}
                                >
                                    <option value={5}>5 miles</option>
                                    <option value={10}>10 miles</option>
                                    <option value={25}>25 miles</option>
                                    <option value={50}>50 miles</option>
                                    <option value={100}>100 miles</option>
                                </select>
                                {!searchCenter && (
                                    <p className="mt-1 text-xs text-gray-500">
                                        Select a city or use My Location to
                                        enable distance search.
                                    </p>
                                )}
                            </div>
                        </div>

                        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                            {/* Country first (per requirements) */}
                            <div>
                                <CountryCombobox
                                    id="country"
                                    label="Country"
                                    value={selectedCountry}
                                    onChange={handleCountryChange}
                                    initialCountryName={defaultCountryName}
                                    disabled={useMyLocation}
                                />
                            </div>
                            <div>
                                <CityCombobox
                                    key={selectedCountry?.code || 'no-country'}
                                    id="city"
                                    label="City"
                                    countryCode={selectedCountry?.code || null}
                                    value={selectedCity}
                                    onChange={handleCityChange}
                                    disabled={useMyLocation}
                                />
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
                                    disabled={useMyLocation}
                                    onChange={(e) =>
                                        setSearchParams((prev) => ({
                                            ...prev,
                                            stateProvince: e.target.value,
                                        }))
                                    }
                                    placeholder="Enter state/province"
                                    className={`mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm ${
                                        useMyLocation
                                            ? 'bg-gray-50 cursor-not-allowed'
                                            : ''
                                    }`}
                                />
                            </div>
                        </div>
                    </div>

                    {/* Age Groups */}
                    <div>
                        <h3 className="text-sm font-medium text-gray-900 mb-1 flex items-center">
                            <UserGroupIcon className="h-5 w-5 mr-2 text-gray-400" />
                            Age Groups
                        </h3>
                        <p className="text-xs text-gray-500 mb-3">
                            Select one or more to filter results.
                        </p>
                        <div className="flex flex-wrap gap-2">
                            {commonAgeGroups.map((ageGroup) => {
                                const isSelected =
                                    searchParams.ageGroups?.includes(ageGroup) ??
                                    false;
                                return (
                                    <button
                                        key={ageGroup}
                                        type="button"
                                        aria-pressed={isSelected}
                                        onClick={() => toggleAgeGroup(ageGroup)}
                                        className={getFilterChipClassName(
                                            isSelected,
                                        )}
                                    >
                                        {ageGroup}
                                        {isSelected && (
                                            <CheckIcon
                                                className="h-4 w-4"
                                                aria-hidden="true"
                                            />
                                        )}
                                    </button>
                                );
                            })}
                        </div>
                    </div>

                    {/* Specialisms */}
                    <div>
                        <h3 className="text-sm font-medium text-gray-900 mb-1 flex items-center">
                            <AcademicCapIcon className="h-5 w-5 mr-2 text-gray-400" />
                            Specialisms
                        </h3>
                        <p className="text-xs text-gray-500 mb-3">
                            Select one or more to filter results.
                        </p>
                        <div className="flex flex-wrap gap-2">
                            {commonSpecialisms.map((specialism) => {
                                const isSelected =
                                    searchParams.specialisms?.includes(
                                        specialism,
                                    ) ?? false;
                                return (
                                    <button
                                        key={specialism}
                                        type="button"
                                        aria-pressed={isSelected}
                                        onClick={() =>
                                            toggleSpecialism(specialism)
                                        }
                                        className={getFilterChipClassName(
                                            isSelected,
                                        )}
                                    >
                                        {specialism}
                                        {isSelected && (
                                            <CheckIcon
                                                className="h-4 w-4"
                                                aria-hidden="true"
                                            />
                                        )}
                                    </button>
                                );
                            })}
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
                <div className="flex items-center justify-between mb-4 gap-4">
                    <h2 className="text-lg font-medium text-gray-900">
                        Search Results ({coaches.length})
                    </h2>
                    {hasSearched && coaches.length > 0 && (
                        <div className="inline-flex rounded-md shadow-sm">
                            <button
                                type="button"
                                onClick={() => setResultsView('list')}
                                className={`px-3 py-2 text-sm font-medium border border-gray-300 rounded-l-md ${
                                    resultsView === 'list'
                                        ? 'bg-gray-100 text-gray-900'
                                        : 'bg-white text-gray-700 hover:bg-gray-50'
                                }`}
                            >
                                List
                            </button>
                            <button
                                type="button"
                                onClick={() => setResultsView('map')}
                                className={`px-3 py-2 text-sm font-medium border border-gray-300 border-l-0 rounded-r-md ${
                                    resultsView === 'map'
                                        ? 'bg-gray-100 text-gray-900'
                                        : 'bg-white text-gray-700 hover:bg-gray-50'
                                }`}
                            >
                                Map
                            </button>
                        </div>
                    )}
                </div>
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
                ) : resultsView === 'map' ? (
                    <div className="space-y-3">
                        {coachesWithCoords.length < coaches.length && (
                            <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-3">
                                <p className="text-sm text-yellow-900">
                                    Some coaches don&apos;t have map coordinates
                                    yet, so they won&apos;t appear on the map
                                    until they update their location.
                                </p>
                            </div>
                        )}
                        <CoachResultsMap
                            coaches={coaches}
                            searchOrigin={searchCenter}
                            searchRadiusMiles={
                                searchCenter ? distanceMiles : null
                            }
                            onSelectCoach={(coachProfileId) => {
                                setResultsView('list');
                                setHighlightedCoachId(coachProfileId);
                                window.setTimeout(() => {
                                    const el = document.getElementById(
                                        `coach-${coachProfileId}`,
                                    );
                                    el?.scrollIntoView({
                                        behavior: 'smooth',
                                        block: 'center',
                                    });
                                }, 0);
                            }}
                        />
                    </div>
                ) : (
                    <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
                        {coaches.map((coach: Coach) => (
                            <div
                                key={coach.coachProfileId}
                                id={`coach-${coach.coachProfileId}`}
                                className={`border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow ${
                                    highlightedCoachId === coach.coachProfileId
                                        ? 'ring-2 ring-indigo-500'
                                        : ''
                                }`}
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
                                        {coach.averageRating !== undefined &&
                                            coach.averageRating !== null && (
                                                <div className="mt-2 flex items-center gap-2">
                                                    <StarRating
                                                        rating={
                                                            coach.averageRating
                                                        }
                                                        size="sm"
                                                        showValue={true}
                                                    />
                                                    {coach.ratingCount !==
                                                        undefined &&
                                                        coach.ratingCount >
                                                            0 && (
                                                            <span className="text-xs text-gray-500">
                                                                (
                                                                {
                                                                    coach.ratingCount
                                                                }{' '}
                                                                {coach.ratingCount ===
                                                                1
                                                                    ? 'rating'
                                                                    : 'ratings'}
                                                                )
                                                            </span>
                                                        )}
                                                </div>
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
                                            (() => {
                                                // Persist current search/results so details page can return to it.
                                                persistSearchState({
                                                    highlightedCoachId:
                                                        coach.coachProfileId,
                                                });
                                                router.push(
                                                    `/app/user/coaches/${coach.coachProfileId}?fromSearch=find-coaches`,
                                                );
                                            })()
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
                                                            coach as Coach,
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
                                                            coach as Coach,
                                                        )
                                                    }
                                                    disabled={connectingCoaches.has(
                                                        coach.coachProfileId,
                                                    )}
                                                    className="flex-1 px-3 py-2 text-sm font-medium text-white bg-indigo-600 rounded-md hover:bg-indigo-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                                                >
                                                    {connectingCoaches.has(
                                                        coach.coachProfileId,
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
