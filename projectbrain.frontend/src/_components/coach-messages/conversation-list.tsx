'use client';

import { useState, useEffect, useCallback, useMemo } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import {
    ConversationSummary,
    CoachMessage,
} from '@/_services/coach-message-service';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { useCoachMessagesSignalR } from '@/_hooks/use-coach-messages-signalr';
import {
    ClockIcon,
    ChatBubbleLeftRightIcon,
} from '@heroicons/react/24/outline';

type SortOption = 'most-recent' | 'alphabetical';

export default function ConversationList() {
    const router = useRouter();
    const pathname = usePathname();
    const [conversations, setConversations] = useState<ConversationSummary[]>(
        []
    );
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [sortBy, setSortBy] = useState<SortOption>('most-recent');
    const [currentUserId, setCurrentUserId] = useState<string>('');

    // Load conversations
    const loadConversations = useCallback(async () => {
        try {
            console.log('Loading conversations');
            setLoading(true);
            setError(null);
            const response = await fetchWithAuth(
                '/api/coach-messages/conversations'
            );
            if (!response.ok) {
                throw new Error('Failed to load conversations');
            }
            const data: ConversationSummary[] = await response.json();
            console.log('Conversations loaded:', data);
            setConversations(data);
        } catch (err) {
            console.error('Error loading conversations:', err);
            setError(
                err instanceof Error
                    ? err.message
                    : 'Failed to load conversations'
            );
        } finally {
            setLoading(false);
        }
    }, []);

    // Load conversations on mount
    useEffect(() => {
        loadConversations();
    }, [loadConversations]);

    // Load current user ID (needed for SignalR message handling)
    useEffect(() => {
        const loadCurrentUser = async () => {
            try {
                const response = await fetchWithAuth('/api/user/me');
                if (response.ok) {
                    const user = await response.json();
                    const userId =
                        (user as any).id ||
                        (user as any).userProfileId ||
                        (user as any).coachProfileId;
                    setCurrentUserId(userId);
                }
            } catch (err) {
                console.error('Error loading current user:', err);
            }
        };
        loadCurrentUser();
    }, []);

    // Handle new message from SignalR - update the conversation list
    const handleNewMessage = useCallback(
        (message: CoachMessage) => {
            setConversations((prev) => {
                const updated = prev.map((conv) => {
                    if (conv.connectionId === message.connectionId) {
                        // Update last message info
                        const snippet =
                            message.messageType === 'text'
                                ? message.content.length > 50
                                    ? message.content.substring(0, 50) + '...'
                                    : message.content
                                : 'Voice message';

                        // Increment unread count if message is from other person
                        const unreadCount =
                            message.senderId !== currentUserId
                                ? conv.unreadCount + 1
                                : conv.unreadCount;

                        return {
                            ...conv,
                            lastMessageSnippet: snippet,
                            lastMessageSenderName:
                                message.sender?.fullName || 'Unknown',
                            lastMessageTimestamp: message.createdAt,
                            unreadCount,
                        };
                    }
                    return conv;
                });
                return updated;
            });
        },
        [currentUserId]
    );

    // Handle typing indicator (not needed for list, but required by hook)
    const handleTypingIndicator = useCallback(() => {
        // No-op for list view
    }, []);

    // Use SignalR to listen to all messages for this user
    // The hook will connect to the user group automatically
    useCoachMessagesSignalR({
        connectionId: '', // Empty connectionId means listen to user group
        onNewMessage: handleNewMessage,
        onTypingIndicator: handleTypingIndicator,
    });

    // Sort conversations
    const sortedConversations = useMemo(() => {
        const sorted = [...conversations];
        if (sortBy === 'most-recent') {
            sorted.sort((a, b) => {
                const timeA = a.lastMessageTimestamp
                    ? new Date(a.lastMessageTimestamp).getTime()
                    : 0;
                const timeB = b.lastMessageTimestamp
                    ? new Date(b.lastMessageTimestamp).getTime()
                    : 0;
                return timeB - timeA; // Most recent first
            });
        } else if (sortBy === 'alphabetical') {
            sorted.sort((a, b) =>
                a.otherPersonName.localeCompare(b.otherPersonName)
            );
        }
        return sorted;
    }, [conversations, sortBy]);

    const formatDate = (dateString?: string): string => {
        if (!dateString) return 'No messages';
        const date = new Date(dateString);
        const now = new Date();
        const diffMs = now.getTime() - date.getTime();
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 1) return 'Just now';
        if (diffMins < 60) return `${diffMins}m ago`;
        if (diffHours < 24) return `${diffHours}h ago`;
        if (diffDays < 7) return `${diffDays}d ago`;

        return date.toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric',
            year:
                date.getFullYear() !== now.getFullYear()
                    ? 'numeric'
                    : undefined,
        });
    };

    const handleConversationClick = (connectionId: string) => {
        // Determine if we're in user or coach section based on current pathname
        if (pathname.startsWith('/app/coach')) {
            router.push(`/app/coach/messages/${connectionId}`);
        } else {
            router.push(`/app/user/messages/${connectionId}`);
        }
    };

    if (loading) {
        return (
            <div className="flex justify-center items-center h-64">
                <div className="text-gray-500">Loading conversations...</div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="flex justify-center items-center h-64">
                <div className="text-red-500">Error: {error}</div>
            </div>
        );
    }

    return (
        <div className="bg-white rounded-lg shadow-sm border border-gray-200">
            {/* Header with sort dropdown */}
            <div className="p-4 border-b border-gray-200 flex items-center justify-between">
                <h2 className="text-lg font-semibold text-gray-900">
                    Messages
                </h2>
                <select
                    value={sortBy}
                    onChange={(e) => setSortBy(e.target.value as SortOption)}
                    className="px-3 py-2 text-sm border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                >
                    <option value="most-recent">Most Recent</option>
                    <option value="alphabetical">Alphabetical</option>
                </select>
            </div>

            {/* Conversation List */}
            {sortedConversations.length === 0 ? (
                <div className="p-8 text-center">
                    <ChatBubbleLeftRightIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                    <p className="text-gray-500">No conversations yet</p>
                </div>
            ) : (
                <ul className="divide-y divide-gray-200">
                    {sortedConversations.map((conversation) => (
                        <li
                            key={conversation.connectionId}
                            className="hover:bg-gray-50 transition-colors cursor-pointer"
                            onClick={() =>
                                handleConversationClick(
                                    conversation.connectionId
                                )
                            }
                        >
                            <div className="p-4">
                                <div className="flex items-start justify-between">
                                    <div className="flex-1 min-w-0">
                                        <div className="flex items-center gap-2">
                                            <p className="text-sm font-medium text-gray-900 truncate">
                                                {conversation.otherPersonName}
                                            </p>
                                            {conversation.unreadCount > 0 && (
                                                <span className="inline-flex items-center justify-center px-2 py-0.5 rounded-full text-xs font-medium bg-indigo-600 text-white">
                                                    {conversation.unreadCount}
                                                </span>
                                            )}
                                        </div>
                                        {conversation.lastMessageSnippet && (
                                            <p className="mt-1 text-sm text-gray-600 truncate">
                                                <span className="font-medium">
                                                    {
                                                        conversation.lastMessageSenderName
                                                    }
                                                    :
                                                </span>{' '}
                                                {
                                                    conversation.lastMessageSnippet
                                                }
                                            </p>
                                        )}
                                        <div className="mt-1 flex items-center text-xs text-gray-500">
                                            <ClockIcon className="h-3 w-3 mr-1 flex-shrink-0" />
                                            {formatDate(
                                                conversation.lastMessageTimestamp
                                            )}
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}
