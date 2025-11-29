'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import {
    XMarkIcon,
    TrashIcon,
    ChatBubbleLeftRightIcon,
    ClockIcon,
} from '@heroicons/react/24/outline';
import { Conversation } from '@/_lib/types';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';

interface ConversationsDrawerProps {
    isOpen: boolean;
    onClose: () => void;
    currentConversationId?: string;
    onConversationSelect: (conversationId: string) => void;
}

export default function ConversationsDrawer({
    isOpen,
    onClose,
    currentConversationId,
    onConversationSelect,
}: ConversationsDrawerProps) {
    const router = useRouter();
    const [conversations, setConversations] = useState<Conversation[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [deletingIds, setDeletingIds] = useState<Set<string>>(new Set());

    useEffect(() => {
        if (isOpen) {
            loadConversations();
        }
    }, [isOpen]);

    const loadConversations = async () => {
        try {
            setIsLoading(true);
            const response = await fetchWithAuth('/api/conversations');
            if (!response.ok) {
                throw new Error('Failed to load conversations');
            }
            const data = await response.json();
            setConversations(data);
        } catch (error) {
            console.error('Failed to load conversations:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const handleDelete = async (
        e: React.MouseEvent<HTMLButtonElement>,
        conversationId: string
    ) => {
        e.preventDefault();
        e.stopPropagation();

        if (
            !confirm(
                'Are you sure you want to delete this conversation? This action cannot be undone.'
            )
        ) {
            return;
        }

        try {
            setDeletingIds((prev) => new Set(prev).add(conversationId));
            const response = await fetchWithAuth(
                `/conversation/${conversationId}`,
                {
                    method: 'DELETE',
                }
            );
            if (!response.ok) {
                throw new Error('Failed to delete conversation');
            }

            // Remove from local state
            setConversations((prev) =>
                prev.filter((c) => c.id !== conversationId)
            );

            // If we deleted the current conversation, navigate to new chat
            if (conversationId === currentConversationId) {
                router.push('/app/user/chat');
                onClose();
            }
        } catch (error) {
            console.error('Failed to delete conversation:', error);
            alert('Failed to delete conversation. Please try again.');
        } finally {
            setDeletingIds((prev) => {
                const next = new Set(prev);
                next.delete(conversationId);
                return next;
            });
        }
    };

    const handleConversationClick = (conversationId: string) => {
        onConversationSelect(conversationId);
        onClose();
    };

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        });
    };

    return (
        <>
            {/* Overlay */}
            {isOpen && (
                <div
                    className="fixed inset-0 bg-black bg-opacity-50 z-40 transition-opacity"
                    onClick={onClose}
                />
            )}

            {/* Drawer */}
            <div
                className={`fixed top-0 right-0 h-full w-80 bg-white shadow-xl z-50 transform transition-transform duration-300 ease-in-out ${
                    isOpen ? 'translate-x-0' : 'translate-x-full'
                }`}
            >
                <div className="flex flex-col h-full">
                    {/* Header */}
                    <div className="flex items-center justify-between p-4 border-b border-gray-200">
                        <h2 className="text-lg font-semibold text-gray-900">
                            Conversations
                        </h2>
                        <button
                            onClick={onClose}
                            className="text-gray-400 hover:text-gray-600 transition-colors"
                            aria-label="Close drawer"
                        >
                            <XMarkIcon className="h-6 w-6" />
                        </button>
                    </div>

                    {/* Content */}
                    <div className="flex-1 overflow-y-auto">
                        {isLoading ? (
                            <div className="flex items-center justify-center py-12">
                                <div className="flex space-x-2">
                                    <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" />
                                    <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce delay-75" />
                                    <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce delay-150" />
                                </div>
                            </div>
                        ) : conversations.length === 0 ? (
                            <div className="text-center py-12 px-4">
                                <ChatBubbleLeftRightIcon className="h-12 w-12 text-gray-300 mx-auto mb-4" />
                                <p className="text-sm text-gray-500">
                                    No conversations yet. Start a new chat to
                                    begin!
                                </p>
                            </div>
                        ) : (
                            <ul className="divide-y divide-gray-200">
                                {conversations.map((conversation) => (
                                    <li
                                        key={conversation.id}
                                        className={`hover:bg-gray-50 transition-colors cursor-pointer ${
                                            conversation.id ===
                                            currentConversationId
                                                ? 'bg-indigo-50'
                                                : ''
                                        }`}
                                        onClick={() =>
                                            handleConversationClick(
                                                conversation.id
                                            )
                                        }
                                    >
                                        <div className="p-4">
                                            <div className="flex items-start justify-between">
                                                <div className="flex-1 min-w-0">
                                                    <p
                                                        className={`text-sm font-medium truncate ${
                                                            conversation.id ===
                                                            currentConversationId
                                                                ? 'text-indigo-900'
                                                                : 'text-gray-900'
                                                        }`}
                                                    >
                                                        {conversation.title}
                                                    </p>
                                                    <div className="mt-1 flex items-center text-xs text-gray-500">
                                                        <ClockIcon className="h-3 w-3 mr-1 flex-shrink-0" />
                                                        <span>
                                                            {formatDate(
                                                                conversation.updatedAt
                                                            )}
                                                        </span>
                                                    </div>
                                                </div>
                                                <button
                                                    onClick={(e) =>
                                                        handleDelete(
                                                            e,
                                                            conversation.id
                                                        )
                                                    }
                                                    disabled={deletingIds.has(
                                                        conversation.id
                                                    )}
                                                    className="ml-2 p-1 text-gray-400 hover:text-red-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex-shrink-0"
                                                    title="Delete conversation"
                                                    aria-label="Delete conversation"
                                                >
                                                    <TrashIcon className="h-4 w-4" />
                                                </button>
                                            </div>
                                        </div>
                                    </li>
                                ))}
                            </ul>
                        )}
                    </div>

                    {/* Footer */}
                    <div className="p-4 border-t border-gray-200">
                        <button
                            onClick={() => {
                                router.push('/app/user/chat');
                                onClose();
                            }}
                            className="w-full px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors text-sm font-medium"
                        >
                            New Conversation
                        </button>
                    </div>
                </div>
            </div>
        </>
    );
}
