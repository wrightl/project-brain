'use client';

import { User } from '@/_lib/types';
import {
    UsersIcon,
    UserCircleIcon,
    MapPinIcon,
    CheckCircleIcon,
} from '@heroicons/react/24/outline';
import ClientDetailModal from './client-detail-modal';
import { useState } from 'react';
import { ClientWithConnectionStatus } from '@/_lib/types';
import { useRouter } from 'next/navigation';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import toast from 'react-hot-toast';

function formatLastActivity(lastActivityAt?: string): string {
    if (!lastActivityAt) {
        return 'Never';
    }

    const date = new Date(lastActivityAt);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) {
        return 'Just now';
    } else if (diffMins < 60) {
        return `${diffMins} minute${diffMins === 1 ? '' : 's'} ago`;
    } else if (diffHours < 24) {
        return `${diffHours} hour${diffHours === 1 ? '' : 's'} ago`;
    } else if (diffDays < 7) {
        return `${diffDays} day${diffDays === 1 ? '' : 's'} ago`;
    } else {
        return date.toLocaleDateString();
    }
}

function isUserOnline(lastActivityAt?: string): boolean {
    if (!lastActivityAt) {
        return false;
    }
    const date = new Date(lastActivityAt);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    return diffMins <= 30;
}

interface ClientsListProps {
    clients: ClientWithConnectionStatus[];
    error: string | null;
    onClientUpdate?: () => void;
}

export default function ClientsList({
    clients,
    error,
    onClientUpdate,
}: ClientsListProps) {
    const router = useRouter();
    const [selectedClient, setSelectedClient] = useState<User | null>(null);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [acceptingClientId, setAcceptingClientId] = useState<string | null>(
        null
    );

    const handleClientClick = (client: User) => {
        setSelectedClient(client);
        setIsModalOpen(true);
    };

    const handleCloseModal = () => {
        setIsModalOpen(false);
        setSelectedClient(null);
    };

    const handleAcceptConnection = async (
        e: React.MouseEvent,
        client: ClientWithConnectionStatus
    ) => {
        e.stopPropagation(); // Prevent opening the modal
        if (acceptingClientId) return;

        setAcceptingClientId(client.user.id);
        try {
            const response = await fetchWithAuth(
                `/api/coach/clients/${client.user.id}/accept`,
                {
                    method: 'POST',
                }
            );

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(
                    errorData.error || 'Failed to accept connection'
                );
            }

            // Refresh the page to get updated data
            router.refresh();
            toast.success('Connection accepted successfully');
        } catch (err) {
            console.error('Error accepting connection:', err);
            toast.error(
                err instanceof Error
                    ? err.message
                    : 'Failed to accept connection'
            );
        } finally {
            setAcceptingClientId(null);
        }
    };

    if (error) {
        return (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                <p className="text-sm text-red-800">{error}</p>
            </div>
        );
    }

    if (clients.length === 0) {
        return (
            <div className="bg-white shadow rounded-lg p-12">
                <div className="text-center">
                    <UsersIcon className="mx-auto h-12 w-12 text-gray-400" />
                    <h3 className="mt-2 text-sm font-medium text-gray-900">
                        No clients yet
                    </h3>
                    <p className="mt-1 text-sm text-gray-500">
                        You don&apos;t have any connected clients. Start by
                        searching for users in your area.
                    </p>
                </div>
            </div>
        );
    }

    return (
        <>
            <div className="bg-white shadow rounded-lg overflow-hidden">
                <div className="px-6 py-4 border-b border-gray-200">
                    <div className="flex items-center justify-between">
                        <h2 className="text-lg font-medium text-gray-900">
                            Clients ({clients.length})
                        </h2>
                    </div>
                </div>
                <ul className="divide-y divide-gray-200">
                    {clients.map((clientWithStatus) => {
                        const client = clientWithStatus.user;
                        const isOnline = isUserOnline(client.lastActivityAt);
                        const isPending =
                            clientWithStatus.connectionStatus === 'pending';
                        const isAccepting = acceptingClientId === client.id;

                        return (
                            <li key={client.id}>
                                <div className="px-6 py-4 hover:bg-gray-50 transition-colors">
                                    <div className="flex items-center justify-between">
                                        <button
                                            onClick={() =>
                                                handleClientClick(client)
                                            }
                                            className="flex-1 flex items-center space-x-4 text-left cursor-pointer"
                                        >
                                            <div className="flex-shrink-0">
                                                <UserCircleIcon className="h-10 w-10 text-gray-400" />
                                            </div>
                                            <div className="flex-1 min-w-0">
                                                <div className="flex items-center space-x-2">
                                                    <p className="text-sm font-medium text-gray-900 truncate">
                                                        {client.fullName}
                                                    </p>
                                                    {isOnline && (
                                                        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800">
                                                            Online
                                                        </span>
                                                    )}
                                                    {isPending && (
                                                        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-yellow-100 text-yellow-800">
                                                            Pending
                                                        </span>
                                                    )}
                                                    {!isPending && (
                                                        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800">
                                                            Connected
                                                        </span>
                                                    )}
                                                </div>
                                                <p className="text-sm text-gray-500 truncate">
                                                    {client.email}
                                                </p>
                                                {client.city && (
                                                    <div className="mt-1 flex items-center text-sm text-gray-500">
                                                        <MapPinIcon className="h-4 w-4 mr-1" />
                                                        {[
                                                            client.city,
                                                            client.stateProvince,
                                                            client.country,
                                                        ]
                                                            .filter(Boolean)
                                                            .join(', ')}
                                                    </div>
                                                )}
                                            </div>
                                        </button>
                                        <div className="flex items-center space-x-4">
                                            <div className="flex-shrink-0 text-right">
                                                <p className="text-sm text-gray-500">
                                                    Last active:{' '}
                                                    {formatLastActivity(
                                                        client.lastActivityAt
                                                    )}
                                                </p>
                                                <p className="text-xs text-gray-400 mt-1">
                                                    Click to view details
                                                </p>
                                            </div>
                                            {isPending && (
                                                <button
                                                    onClick={(e) =>
                                                        handleAcceptConnection(
                                                            e,
                                                            clientWithStatus
                                                        )
                                                    }
                                                    disabled={isAccepting}
                                                    className="flex items-center px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-md hover:bg-indigo-700 transition-colors disabled:bg-gray-400 disabled:cursor-not-allowed"
                                                >
                                                    <CheckCircleIcon className="h-4 w-4 mr-2" />
                                                    {isAccepting
                                                        ? 'Accepting...'
                                                        : 'Accept'}
                                                </button>
                                            )}
                                        </div>
                                    </div>
                                </div>
                            </li>
                        );
                    })}
                </ul>
            </div>

            <ClientDetailModal
                client={selectedClient}
                isOpen={isModalOpen}
                onClose={handleCloseModal}
            />
        </>
    );
}
