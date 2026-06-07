'use client';

import { useRouter, useSearchParams } from 'next/navigation';
import {
    MapPinIcon,
    EnvelopeIcon,
    AcademicCapIcon,
    UserGroupIcon,
    SparklesIcon,
    CheckCircleIcon,
    ClockIcon,
} from '@heroicons/react/24/outline';
import { Coach, SubscriptionUserType } from '@/_lib/types';
import { useState, useEffect } from 'react';
import AvailabilityBadge from '@/_components/coach/availability-badge';
import StarRating from '@/_components/coach/star-rating';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import Link from 'next/link';

interface ConnectionStatus {
    status: 'none' | 'pending' | 'connected';
    connectionId?: string;
    requestedAt?: string;
    respondedAt?: string;
    requestedBy?: SubscriptionUserType;
}

interface CoachDetailViewProps {
    coach: Coach;
}

export default function CoachDetailView({ coach }: CoachDetailViewProps) {
    const router = useRouter();
    const searchParams = useSearchParams();
    const [connectionStatus, setConnectionStatus] =
        useState<ConnectionStatus | null>(null);
    const [loadingConnectionStatus, setLoadingConnectionStatus] =
        useState(true);
    const [isConnecting, setIsConnecting] = useState(false);
    const [connectError, setConnectError] = useState<string | null>(null);

    useEffect(() => {
        const fetchConnectionStatus = async () => {
            try {
                setLoadingConnectionStatus(true);
                const response = await fetchWithAuth(
                    `/api/coaches/${coach.coachProfileId}/connection-status`,
                );
                if (response.ok) {
                    const status: ConnectionStatus = await response.json();
                    setConnectionStatus(status);
                } else {
                    setConnectionStatus({ status: 'none' });
                }
            } catch (err) {
                console.error('Error fetching connection status:', err);
                setConnectionStatus({ status: 'none' });
            } finally {
                setLoadingConnectionStatus(false);
            }
        };

        fetchConnectionStatus();
    }, [coach.coachProfileId]);

    const handleContactCoach = () => {
        if (
            connectionStatus?.status === 'connected' &&
            connectionStatus.connectionId
        ) {
            router.push(`/app/user/messages/${connectionStatus.connectionId}`);
        }
    };

    const handleConnectCoach = async () => {
        if (isConnecting) return;
        if (connectionStatus?.status !== 'none') return;

        setIsConnecting(true);
        setConnectError(null);
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
            setConnectionStatus({
                status: data.status === 'connected' ? 'connected' : 'pending',
                connectionId: data.id,
                requestedAt: data.requestedAt,
            });
        } catch (err) {
            setConnectError(
                err instanceof Error
                    ? err.message
                    : 'Failed to send connection request',
            );
        } finally {
            setIsConnecting(false);
        }
    };

    const renderConnectionAction = (variant: 'header' | 'footer') => {
        const status = connectionStatus?.status ?? 'none';

        if (status === 'connected') {
            return (
                <button
                    onClick={handleContactCoach}
                    disabled={!connectionStatus?.connectionId}
                    className="px-6 py-3 bg-indigo-600 text-white font-medium rounded-md hover:bg-indigo-700 transition-colors disabled:bg-gray-300 disabled:cursor-not-allowed"
                >
                    {variant === 'header' ? 'Message Coach' : 'Start Conversation'}
                </button>
            );
        }

        if (status === 'pending') {
            return (
                <button
                    disabled
                    className="px-6 py-3 text-gray-600 bg-gray-200 font-medium rounded-md cursor-not-allowed"
                >
                    Connection Pending
                </button>
            );
        }

        return (
            <button
                onClick={handleConnectCoach}
                disabled={isConnecting || loadingConnectionStatus}
                className="px-6 py-3 bg-indigo-600 text-white font-medium rounded-md hover:bg-indigo-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
                {isConnecting ? 'Connecting...' : 'Connect with Coach'}
            </button>
        );
    };

    return (
        <div className="max-w-4xl mx-auto space-y-6">
            {searchParams.get('fromSearch') === 'find-coaches' && (
                <div className="flex items-center">
                    <Link
                        href="/app/user/find-coaches?restore=1"
                        className="text-sm font-medium text-indigo-600 hover:text-indigo-800 underline"
                    >
                        Return to Search
                    </Link>
                </div>
            )}
            {/* Header */}
            <div className="bg-white shadow rounded-lg p-6">
                <div className="flex items-start justify-between">
                    <div className="flex-1">
                        <div className="flex items-center gap-3">
                            <h1 className="text-3xl font-bold text-gray-900">
                                {coach.fullName}
                            </h1>
                            {coach.availabilityStatus && (
                                <AvailabilityBadge
                                    status={coach.availabilityStatus}
                                />
                            )}
                            {!loadingConnectionStatus && connectionStatus && (
                                <div className="ml-2">
                                    {connectionStatus.status ===
                                        'connected' && (
                                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                                            <CheckCircleIcon className="h-3 w-3 mr-1" />
                                            Connected
                                        </span>
                                    )}
                                    {connectionStatus.status === 'pending' && (
                                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
                                            <ClockIcon className="h-3 w-3 mr-1" />
                                            Connection Pending
                                        </span>
                                    )}
                                </div>
                            )}
                        </div>
                        {coach.city && (
                            <p className="mt-2 text-gray-600 flex items-center">
                                <MapPinIcon className="h-5 w-5 mr-2 text-gray-400" />
                                {[
                                    coach.city,
                                    coach.stateProvince,
                                    coach.country,
                                ]
                                    .filter(Boolean)
                                    .join(', ')}
                            </p>
                        )}
                        {coach.email && (
                            <p className="mt-1 text-gray-600 flex items-center">
                                <EnvelopeIcon className="h-5 w-5 mr-2 text-gray-400" />
                                {coach.email}
                            </p>
                        )}
                        {coach.averageRating !== undefined &&
                            coach.averageRating !== null && (
                                <div className="mt-3 flex items-center gap-3">
                                    <StarRating
                                        rating={coach.averageRating}
                                        size="md"
                                        showValue={true}
                                    />
                                    {coach.ratingCount !== undefined &&
                                        coach.ratingCount > 0 && (
                                            <Link
                                                href={`/app/user/coaches/${coach.coachProfileId}/ratings`}
                                                className="text-sm text-indigo-600 hover:text-indigo-800 underline"
                                            >
                                                View all {coach.ratingCount}{' '}
                                                {coach.ratingCount === 1
                                                    ? 'rating'
                                                    : 'ratings'}
                                            </Link>
                                        )}
                                </div>
                            )}
                    </div>
                    {renderConnectionAction('header')}
                </div>
            </div>

            {/* Qualifications */}
            {coach.qualifications.length > 0 && (
                <div className="bg-white shadow rounded-lg p-6">
                    <h2 className="text-xl font-semibold text-gray-900 mb-4 flex items-center">
                        <AcademicCapIcon className="h-6 w-6 mr-2 text-indigo-600" />
                        Qualifications
                    </h2>
                    <ul className="space-y-2">
                        {coach.qualifications.map((qualification, index) => (
                            <li
                                key={index}
                                className="flex items-start text-gray-700"
                            >
                                <span className="mr-2 text-indigo-600">•</span>
                                <span>{qualification}</span>
                            </li>
                        ))}
                    </ul>
                </div>
            )}

            {/* Specialisms */}
            {coach.specialisms.length > 0 && (
                <div className="bg-white shadow rounded-lg p-6">
                    <h2 className="text-xl font-semibold text-gray-900 mb-4 flex items-center">
                        <SparklesIcon className="h-6 w-6 mr-2 text-indigo-600" />
                        Specialisms
                    </h2>
                    <div className="flex flex-wrap gap-2">
                        {coach.specialisms.map((specialism) => (
                            <span
                                key={specialism}
                                className="px-3 py-1 bg-indigo-100 text-indigo-800 text-sm font-medium rounded-full"
                            >
                                {specialism}
                            </span>
                        ))}
                    </div>
                </div>
            )}

            {/* Age Groups */}
            {coach.ageGroups.length > 0 && (
                <div className="bg-white shadow rounded-lg p-6">
                    <h2 className="text-xl font-semibold text-gray-900 mb-4 flex items-center">
                        <UserGroupIcon className="h-6 w-6 mr-2 text-indigo-600" />
                        Age Groups
                    </h2>
                    <div className="flex flex-wrap gap-2">
                        {coach.ageGroups.map((ageGroup) => (
                            <span
                                key={ageGroup}
                                className="px-3 py-1 bg-green-100 text-green-800 text-sm font-medium rounded-full"
                            >
                                {ageGroup}
                            </span>
                        ))}
                    </div>
                </div>
            )}

            {/* Contact Section */}
            <div className="bg-indigo-50 border border-indigo-200 rounded-lg p-6">
                <h3 className="text-lg font-semibold text-indigo-900 mb-2">
                    Ready to get started?
                </h3>
                <p className="text-sm text-indigo-700 mb-4">
                    {connectionStatus?.status === 'connected'
                        ? `You're connected with ${coach.fullName}. Start a conversation via text or voice.`
                        : connectionStatus?.status === 'pending'
                        ? `Your connection request to ${coach.fullName} is pending. Once accepted, you can start chatting.`
                        : `Connect with ${coach.fullName} to discuss how they can help you. Once connected, you can chat via text or voice.`}
                </p>
                {connectError && (
                    <p className="text-sm text-red-700 mb-4">{connectError}</p>
                )}
                {renderConnectionAction('footer')}
            </div>
        </div>
    );
}
