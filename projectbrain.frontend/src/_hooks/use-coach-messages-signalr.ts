import { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { CoachMessage } from '@/_services/coach-message-service';

interface UseCoachMessagesSignalRProps {
    connectionId: string;
    onNewMessage: (message: CoachMessage) => void;
    onTypingIndicator: (senderId: string, typing: boolean) => void;
}

export function useCoachMessagesSignalR({
    connectionId,
    onNewMessage,
    onTypingIndicator,
}: UseCoachMessagesSignalRProps) {
    const [connection, setConnection] = useState<signalR.HubConnection | null>(
        null
    );
    const [isConnected, setIsConnected] = useState(false);
    const connectionRef = useRef<signalR.HubConnection | null>(null);

    // Use refs to store callbacks to avoid dependency issues
    const onNewMessageRef = useRef(onNewMessage);
    const onTypingIndicatorRef = useRef(onTypingIndicator);

    // Update refs when callbacks change
    useEffect(() => {
        onNewMessageRef.current = onNewMessage;
        onTypingIndicatorRef.current = onTypingIndicator;
    }, [onNewMessage, onTypingIndicator]);

    useEffect(() => {
        let isMounted = true;
        const apiUrl =
            process.env.NEXT_PUBLIC_API_SERVER_URL || 'https://localhost:7585';
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${apiUrl}/hubs/coach-messages`, {
                accessTokenFactory: async () => {
                    try {
                        const response = await fetch('/api/auth/token');
                        if (!response.ok) {
                            throw new Error('Failed to get access token');
                        }
                        const data = await response.json();
                        return data.token;
                    } catch (error) {
                        console.error(
                            'Error getting access token for SignalR:',
                            error
                        );
                        throw error;
                    }
                },
            })
            .withAutomaticReconnect()
            .build();

        newConnection
            .start()
            .then(() => {
                if (!isMounted) return;
                console.log('SignalR Connected');
                setIsConnected(true);
                // Join conversation group only if connectionId is provided
                // If empty, user is automatically added to user_{userId} group on connect
                if (connectionId) {
                    newConnection
                        .invoke('JoinConversation', connectionId)
                        .catch((err) => {
                            console.error('Error joining conversation:', err);
                        });
                }
            })
            .catch((err) => {
                if (!isMounted) return;
                console.error('SignalR Connection Error: ', err);
                setIsConnected(false);
            });

        newConnection.on('NewMessage', (message: CoachMessage) => {
            onNewMessageRef.current(message);
        });

        newConnection.on(
            'TypingIndicator',
            (senderId: string, typing: boolean) => {
                onTypingIndicatorRef.current(senderId, typing);
            }
        );

        newConnection.onreconnecting(() => {
            if (isMounted) {
                setIsConnected(false);
            }
        });

        newConnection.onreconnected(() => {
            if (!isMounted) return;
            setIsConnected(true);
            // Rejoin conversation group only if connectionId is provided
            if (connectionId) {
                newConnection
                    .invoke('JoinConversation', connectionId)
                    .catch((err) => {
                        console.error('Error rejoining conversation:', err);
                    });
            }
        });

        newConnection.onclose(() => {
            if (isMounted) {
                setIsConnected(false);
            }
        });

        setConnection(newConnection);
        connectionRef.current = newConnection;

        return () => {
            isMounted = false;

            // Only stop if connection is in a state that can be stopped
            const state = newConnection.state;
            if (state === signalR.HubConnectionState.Connected) {
                // Leave conversation group only if connectionId is provided
                if (connectionId) {
                    newConnection
                        .invoke('LeaveConversation', connectionId)
                        .catch(console.error);
                }

                // Stop the connection, but handle the error gracefully
                newConnection.stop().catch((err) => {
                    // Ignore the specific error about stopping before start
                    const errorMessage = err?.message || err?.toString() || '';
                    if (
                        !errorMessage.includes('before stop') &&
                        !errorMessage.includes('Failed to start')
                    ) {
                        console.error(
                            'Error stopping SignalR connection:',
                            err
                        );
                    }
                });
            }
        };
    }, [connectionId]);

    const sendTypingIndicator = (typing: boolean) => {
        if (connectionRef.current && isConnected && connectionId) {
            connectionRef.current
                .invoke('SendTypingIndicator', connectionId, typing)
                .catch((err) => {
                    console.error('Error sending typing indicator:', err);
                });
        }
    };

    return {
        connection: connectionRef.current,
        isConnected,
        sendTypingIndicator,
    };
}
