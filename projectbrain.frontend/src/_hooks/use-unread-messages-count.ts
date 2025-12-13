import { useState, useEffect } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { ConversationSummary } from '@/_services/coach-message-service';

export function useUnreadMessagesCount() {
    const [unreadCount, setUnreadCount] = useState<number>(0);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const loadUnreadCount = async () => {
            try {
                const response = await fetchWithAuth('/api/coach-messages/conversations');
                if (response.ok) {
                    const conversations: ConversationSummary[] = await response.json();
                    const total = conversations.reduce((sum, conv) => sum + conv.unreadCount, 0);
                    setUnreadCount(total);
                }
            } catch (err) {
                console.error('Error loading unread count:', err);
            } finally {
                setLoading(false);
            }
        };

        loadUnreadCount();
        
        // Refresh every 30 seconds
        const interval = setInterval(loadUnreadCount, 30000);
        
        return () => clearInterval(interval);
    }, []);

    return { unreadCount, loading };
}

