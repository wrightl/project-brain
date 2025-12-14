'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import {
    UserGroupIcon,
    ChatBubbleLeftRightIcon,
    XMarkIcon,
    PaperAirplaneIcon,
    ClockIcon,
    StarIcon,
} from '@heroicons/react/24/outline';
import { useConnections, useConversations, useDeleteConnection } from '@/_hooks/queries/use-connections';
import toast from 'react-hot-toast';
import { SkeletonCard, SkeletonList } from '@/_components/ui/skeleton';

export default function ConnectionsPageContent() {
    const router = useRouter();
    const { data: connectionsResponse, isLoading: connectionsLoading, error: connectionsError } = useConnections();
    const connections = connectionsResponse?.items || [];
    const { data: conversations = [], isLoading: conversationsLoading, error: conversationsError } = useConversations();
    const deleteConnection = useDeleteConnection();
    const [deletingIds, setDeletingIds] = useState<Set<string>>(new Set());

    const loading = connectionsLoading || conversationsLoading;
    const error = connectionsError || conversationsError;

    const handleDeleteConnection = async (connectionId: string) => {
        if (!confirm('Are you sure you want to remove this connection?')) {
            return;
        }

        try {
            setDeletingIds((prev) => new Set(prev).add(connectionId));
            await deleteConnection.mutateAsync(connectionId);
            toast.success('Connection removed successfully');
        } catch (err) {
            console.error('Error deleting connection:', err);
            toast.error('Failed to delete connection. Please try again.');
        } finally {
            setDeletingIds((prev) => {
                const next = new Set(prev);
                next.delete(connectionId);
                return next;
            });
        }
    };

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-GB', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
        });
    };

    const handleRateCoach = (coachProfileId?: string) => {
        if (!coachProfileId) {
            toast.error('Coach profile not available');
            return;
        }
        router.push(`/app/user/coaches/${coachProfileId}/ratings`);
    };

    const totalConnections = connections.filter(
        (c) => c.status === 'accepted' || c.status === 'pending'
    ).length;
    const totalMessages = conversations.length; // Number of active conversation threads

    const stats = [
        {
            name: 'Total Connections',
            value: totalConnections.toString(),
            icon: UserGroupIcon,
        },
        {
            name: 'Total Messages',
            value: totalMessages.toString(),
            icon: ChatBubbleLeftRightIcon,
        },
    ];

    const quickActions = [
        {
            title: 'Find a Coach',
            description: 'Search for coaches by location, age groups, and specialisms',
            href: '/app/user/find-coaches',
            icon: UserGroupIcon,
            color: 'bg-purple-500',
        },
        {
            title: 'Messages',
            description: 'View and manage your messages with coaches',
            href: '/app/user/messages',
            icon: ChatBubbleLeftRightIcon,
            color: 'bg-indigo-500',
        },
    ];

    const connectedCoaches = connections.filter(
        (c) => c.status === 'accepted' || c.status === 'pending'
    );

    if (loading) {
        return (
            <div className="space-y-8">
                <div>
                    <h1 className="text-3xl font-bold text-gray-900">
                        My Network
                    </h1>
                    <p className="mt-2 text-sm text-gray-600">
                        Manage your connections with coaches
                    </p>
                </div>
                <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                    <SkeletonCard />
                    <SkeletonCard />
                </div>
                <SkeletonList count={3} />
            </div>
        );
    }

    if (error) {
        return (
            <div className="space-y-8">
                <div>
                    <h1 className="text-3xl font-bold text-gray-900">
                        My Network
                    </h1>
                    <p className="mt-2 text-sm text-gray-600">
                        Manage your connections with coaches
                    </p>
                </div>
                <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                    <p className="text-red-800">
                        {error instanceof Error ? error.message : 'Failed to load connections'}
                    </p>
                </div>
            </div>
        );
    }

    return (
        <div className="space-y-8">
            <div>
                <h1 className="text-3xl font-bold text-gray-900">My Network</h1>
                <p className="mt-2 text-sm text-gray-600">
                    Manage your connections with coaches
                </p>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                {stats.map((stat) => {
                    const Icon = stat.icon;
                    return (
                        <div
                            key={stat.name}
                            className="bg-white overflow-hidden shadow rounded-lg"
                        >
                            <div className="p-5">
                                <div className="flex items-center">
                                    <div className="flex-shrink-0">
                                        <Icon
                                            className="h-6 w-6 text-gray-400"
                                            aria-hidden="true"
                                        />
                                    </div>
                                    <div className="ml-5 w-0 flex-1">
                                        <dl>
                                            <dt className="text-sm font-medium text-gray-500 truncate">
                                                {stat.name}
                                            </dt>
                                            <dd className="text-lg font-semibold text-gray-900">
                                                {stat.value}
                                            </dd>
                                        </dl>
                                    </div>
                                </div>
                            </div>
                        </div>
                    );
                })}
            </div>

            {/* Quick Actions */}
            <div>
                <h2 className="text-xl font-semibold text-gray-900 mb-4">
                    Quick Actions
                </h2>
                <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-2">
                    {quickActions.map((action) => {
                        const Icon = action.icon;
                        return (
                            <Link
                                key={action.href}
                                href={action.href || ''}
                                className="relative group bg-white p-6 rounded-lg shadow hover:shadow-lg transition-shadow"
                            >
                                <div>
                                    <span
                                        className={`${action.color} rounded-lg inline-flex p-3 ring-4 ring-white`}
                                    >
                                        <Icon
                                            className="h-6 w-6 text-white"
                                            aria-hidden="true"
                                        />
                                    </span>
                                </div>
                                <div className="mt-4">
                                    <h3 className="text-lg font-medium text-gray-900 group-hover:text-indigo-600">
                                        {action.title}
                                    </h3>
                                    <p className="mt-2 text-sm text-gray-500">
                                        {action.description}
                                    </p>
                                </div>
                            </Link>
                        );
                    })}
                </div>
            </div>

            {/* Connected Coaches List */}
            <div>
                <h2 className="text-xl font-semibold text-gray-900 mb-4">
                    My Coaches
                </h2>
                {connectedCoaches.length === 0 ? (
                    <div className="bg-white shadow rounded-lg p-8 text-center">
                        <UserGroupIcon className="mx-auto h-12 w-12 text-gray-400" />
                        <h3 className="mt-2 text-sm font-medium text-gray-900">
                            No connections yet
                        </h3>
                        <p className="mt-1 text-sm text-gray-500">
                            Start by finding a coach to connect with.
                        </p>
                        <div className="mt-6">
                            <Link
                                href="/app/user/find-coaches"
                                className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
                            >
                                Find a Coach
                            </Link>
                        </div>
                    </div>
                ) : (
                    <div className="bg-white shadow rounded-lg overflow-hidden">
                        <ul className="divide-y divide-gray-200">
                            {connectedCoaches.map((connection) => {
                                const isConnected = connection.status === 'accepted';
                                const isPending = connection.status === 'pending';
                                const isDeleting = deletingIds.has(connection.id);
                                const conversation = conversations.find(
                                    (c) => c.connectionId === connection.id
                                );

                                return (
                                    <li key={connection.id} className="p-6">
                                        <div className="flex items-center justify-between">
                                            <div className="flex-1">
                                                <div className="flex items-center">
                                                    <h3 className="text-lg font-medium text-gray-900">
                                                        {connection.userName || connection.coachName || 'Unknown Coach'}
                                                    </h3>
                                                    <span
                                                        className={`ml-3 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                                                            isConnected
                                                                ? 'bg-green-100 text-green-800'
                                                                : 'bg-yellow-100 text-yellow-800'
                                                        }`}
                                                    >
                                                        {isConnected
                                                            ? 'Connected'
                                                            : 'Pending'}
                                                    </span>
                                                </div>
                                                <div className="mt-2 flex items-center text-sm text-gray-500">
                                                    {isConnected ? (
                                                        <>
                                                            <ClockIcon className="h-4 w-4 mr-1" />
                                                            Connected on{' '}
                                                            {connection.respondedAt
                                                                ? formatDate(
                                                                      connection.respondedAt
                                                                  )
                                                                : 'N/A'}
                                                        </>
                                                    ) : (
                                                        <>
                                                            <ClockIcon className="h-4 w-4 mr-1" />
                                                            Requested on{' '}
                                                            {formatDate(
                                                                connection.requestedAt
                                                            )}
                                                        </>
                                                    )}
                                                </div>
                                            </div>
                                            <div className="ml-4 flex items-center space-x-2">
                                                {isConnected && (
                                                    <>
                                                        <button
                                                            onClick={() =>
                                                                router.push(
                                                                    `/app/user/messages/${connection.id}`
                                                                )
                                                            }
                                                            className="inline-flex items-center px-3 py-2 border border-transparent text-sm leading-4 font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
                                                        >
                                                            <PaperAirplaneIcon className="h-4 w-4 mr-1" />
                                                            Message
                                                        </button>
                                                        <button
                                                            onClick={() =>
                                                                handleRateCoach(
                                                                    connection.coachProfileId
                                                                )
                                                            }
                                                            disabled={!connection.coachProfileId}
                                                            className="inline-flex items-center px-3 py-2 border border-transparent text-sm leading-4 font-medium rounded-md text-white bg-yellow-600 hover:bg-yellow-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-yellow-500 disabled:opacity-50"
                                                        >
                                                            <StarIcon className="h-4 w-4 mr-1" />
                                                            Rate Coach
                                                        </button>
                                                        <button
                                                            onClick={() =>
                                                                handleDeleteConnection(
                                                                    connection.id
                                                                )
                                                            }
                                                            disabled={isDeleting}
                                                            className="inline-flex items-center px-3 py-2 border border-gray-300 text-sm leading-4 font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50"
                                                        >
                                                            <XMarkIcon className="h-4 w-4 mr-1" />
                                                            {isDeleting
                                                                ? 'Removing...'
                                                                : 'Remove'}
                                                        </button>
                                                    </>
                                                )}
                                                {isPending && (
                                                    <button
                                                        onClick={() =>
                                                            handleDeleteConnection(
                                                                connection.id
                                                            )
                                                        }
                                                        disabled={isDeleting}
                                                        className="inline-flex items-center px-3 py-2 border border-gray-300 text-sm leading-4 font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50"
                                                    >
                                                        <XMarkIcon className="h-4 w-4 mr-1" />
                                                        {isDeleting
                                                            ? 'Cancelling...'
                                                            : 'Cancel'}
                                                    </button>
                                                )}
                                            </div>
                                        </div>
                                    </li>
                                );
                            })}
                        </ul>
                    </div>
                )}
            </div>
        </div>
    );
}

