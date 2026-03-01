'use client';

import { useState, useEffect, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { ConversationSummary } from '@/_services/coach-message-service';

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
        const apiUrl =
            process.env.NEXT_PUBLIC_API_SERVER_URL || 'https://localhost:7585';
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${apiUrl}/hubs/coach-messages`, {
                accessTokenFactory: async () => {
                    const response = await fetch('/api/auth/token');
                    if (!response.ok) {
                        throw new Error('Failed to get access token');
                    }
                    const data = await response.json();
                    return data.token;
                },
            })
            .withAutomaticReconnect()
            .build();

        connection.on('UnreadCountUpdated', (totalUnread: number) => {
            if (isMounted) {
                setUnreadCount(totalUnread);
            }
        });

        connection.onreconnected(() => {
            if (isMounted) {
                loadUnreadCount();
            }
        });

        connection.start().catch((err) => {
            if (isMounted) {
                console.error('SignalR connection for unread count:', err);
            }
        });

        return () => {
            isMounted = false;
            if (connection.state === signalR.HubConnectionState.Connected) {
                connection.stop().catch(() => {});
            }
        };
    }, [loadUnreadCount]);

    return { unreadCount, loading };
}
