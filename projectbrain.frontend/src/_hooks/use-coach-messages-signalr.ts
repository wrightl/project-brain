import { useEffect, useRef, useState } from 'react';
import { CoachMessage } from '@/_services/coach-message-service';
import {
    acquireCoachMessagesHub,
    getCoachMessagesHubState,
    invokeCoachMessagesHub,
    releaseCoachMessagesHub,
    subscribeCoachMessagesHubEvent,
} from '@/_lib/coach-messages-hub-client';

interface UseCoachMessagesSignalRProps {
    connectionId: string;
    onNewMessage: (message: CoachMessage) => void;
    onTypingIndicator: (typing: boolean) => void;
    onMessageDelivered?: (message: CoachMessage) => void;
    onMessageRead?: (message: CoachMessage) => void;
}

export function useCoachMessagesSignalR({
    connectionId,
    onNewMessage,
    onTypingIndicator,
    onMessageDelivered,
    onMessageRead,
}: UseCoachMessagesSignalRProps) {
    const [isConnected, setIsConnected] = useState(false);
    const connectionIdRef = useRef(connectionId);

    const onNewMessageRef = useRef(onNewMessage);
    const onTypingIndicatorRef = useRef(onTypingIndicator);
    const onMessageDeliveredRef = useRef(onMessageDelivered);
    const onMessageReadRef = useRef(onMessageRead);

    useEffect(() => {
        onNewMessageRef.current = onNewMessage;
        onTypingIndicatorRef.current = onTypingIndicator;
        onMessageDeliveredRef.current = onMessageDelivered;
        onMessageReadRef.current = onMessageRead;
    }, [onNewMessage, onTypingIndicator, onMessageDelivered, onMessageRead]);

    useEffect(() => {
        let isMounted = true;
        connectionIdRef.current = connectionId;

        const joinConversation = async () => {
            if (!connectionId) return;
            try {
                await invokeCoachMessagesHub('JoinConversation', connectionId);
            } catch (err) {
                console.error('Error joining conversation:', err);
            }
        };

        const leaveConversation = async (id: string) => {
            if (!id) return;
            try {
                await invokeCoachMessagesHub('LeaveConversation', id);
            } catch (err) {
                console.error('Error leaving conversation:', err);
            }
        };

        acquireCoachMessagesHub()
            .then((hub) => {
                if (!isMounted) return;
                setIsConnected(getCoachMessagesHubState() === 'Connected');
                return joinConversation();
            })
            .catch((err) => {
                if (isMounted) {
                    console.error('SignalR Connection Error: ', err);
                    setIsConnected(false);
                }
            });

        const unsubscribers = [
            subscribeCoachMessagesHubEvent('NewMessage', (message) => {
                onNewMessageRef.current(message as CoachMessage);
            }),
            subscribeCoachMessagesHubEvent('TypingIndicator', (typing) => {
                onTypingIndicatorRef.current(typing as boolean);
            }),
            subscribeCoachMessagesHubEvent('MessageDelivered', (message) => {
                onMessageDeliveredRef.current?.(message as CoachMessage);
            }),
            subscribeCoachMessagesHubEvent('MessageRead', (message) => {
                onMessageReadRef.current?.(message as CoachMessage);
            }),
        ];

        return () => {
            isMounted = false;
            unsubscribers.forEach((unsubscribe) => unsubscribe());
            void leaveConversation(connectionIdRef.current);
            releaseCoachMessagesHub();
        };
    }, [connectionId]);

    const sendTypingIndicator = (typing: boolean) => {
        if (isConnected && connectionId) {
            void invokeCoachMessagesHub(
                'SendTypingIndicator',
                connectionId,
                typing,
            ).catch((err) => {
                console.error('Error sending typing indicator:', err);
            });
        }
    };

    return {
        isConnected,
        sendTypingIndicator,
    };
}
