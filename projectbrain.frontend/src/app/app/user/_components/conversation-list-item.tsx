'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState, useEffect } from 'react';
import {
    ChatBubbleLeftRightIcon,
    ClockIcon,
    TrashIcon,
} from '@heroicons/react/24/outline';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { Conversation } from '@/_lib/types';

interface ConversationListItemProps {
    conversation: Conversation;
}

export function ConversationListItem({
    conversation,
}: ConversationListItemProps) {
    const router = useRouter();
    const [isDeleting, setIsDeleting] = useState(false);
    const [formattedDate, setFormattedDate] = useState<string>('');
    const [isMounted, setIsMounted] = useState(false);

    useEffect(() => {
        setIsMounted(true);
        // Format date with time on client side to avoid hydration mismatch
        const date = new Date(conversation.updatedAt);
        setFormattedDate(
            date.toLocaleString('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit',
            })
        );
    }, [conversation.updatedAt]);

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
            setIsDeleting(true);
            const response = await fetchWithAuth(
                `/api/conversations/${conversationId}`,
                {
                    method: 'DELETE',
                }
            );
            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(
                    errorData.error || 'Failed to delete conversation'
                );
            }
            // Refresh the page to update the conversation list
            router.refresh();
        } catch (err) {
            alert(
                err instanceof Error
                    ? err.message
                    : 'Failed to delete conversation'
            );
            setIsDeleting(false);
        }
    };

    return (
        <li>
            <Link
                href={`/app/user/chat/${conversation.id}`}
                className="block hover:bg-gray-50 transition-colors"
            >
                <div className="px-6 py-4">
                    <div className="flex items-center justify-between">
                        <div className="flex-1 min-w-0">
                            <p className="text-sm font-medium text-gray-900 truncate">
                                {conversation.title}
                            </p>
                            <div className="mt-1 flex items-center text-xs text-gray-500">
                                <ClockIcon className="h-4 w-4 mr-1 flex-shrink-0" />
                                {isMounted ? (
                                    formattedDate
                                ) : (
                                    <span suppressHydrationWarning>
                                        {new Date(
                                            conversation.updatedAt
                                        ).toLocaleDateString('en-US', {
                                            year: 'numeric',
                                            month: 'short',
                                            day: 'numeric',
                                        })}
                                    </span>
                                )}
                            </div>
                        </div>
                        <div className="flex items-center gap-2 ml-4">
                            <button
                                onClick={(e) =>
                                    handleDelete(e, conversation.id)
                                }
                                disabled={isDeleting}
                                className="p-1 text-gray-400 hover:text-red-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                                title="Delete conversation"
                                aria-label="Delete conversation"
                            >
                                <TrashIcon className="h-5 w-5" />
                            </button>
                            <ChatBubbleLeftRightIcon className="h-5 w-5 text-gray-400 flex-shrink-0" />
                        </div>
                    </div>
                </div>
            </Link>
        </li>
    );
}
