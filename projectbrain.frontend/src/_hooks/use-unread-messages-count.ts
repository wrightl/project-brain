'use client';

import { useState, useEffect, useCallback } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { ConversationSummary } from '@/_services/coach-message-service';
import {
    acquireCoachMessagesHub,
    releaseCoachMessagesHub,
    subscribeCoachMessagesHubEvent,
} from '@/_lib/coach-messages-hub-client';

export function useUnreadMessagesCount() {
    const [unreadCount, setUnreadCount] = useState<number>(0);
    const [loading, setLoading] = useState(true);

    const loadUnreadCount = useCallback(async () => {
        try {
            const response = await fetchWithAuth(
                '/api/coach-messages/conversations',
            );
            if (response.ok) {
                const conversations: ConversationSummary[] =
                    await response.json();
                const total = conversations.reduce(
                    (sum, conv) => sum + conv.unreadCount,
                    0,
                );
                setUnreadCount(total);
            }
        } catch (err) {
            console.error('Error loading unread count:', err);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        loadUnreadCount();
    }, [loadUnreadCount]);

    useEffect(() => {
        let isMounted = true;

        acquireCoachMessagesHub().catch((err) => {
            if (isMounted) {
                console.error('SignalR connection for unread count:', err);
            }
        });

        const unsubscribe = subscribeCoachMessagesHubEvent(
            'UnreadCountUpdated',
            (totalUnread) => {
                if (isMounted) {
                    setUnreadCount(totalUnread as number);
                }
            },
        );

        return () => {
            isMounted = false;
            unsubscribe();
            releaseCoachMessagesHub();
        };
    }, [loadUnreadCount]);

    return { unreadCount, loading };
}
