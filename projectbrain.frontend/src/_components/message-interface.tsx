'use client';

import { useState, useEffect, useRef, useCallback } from 'react';
import { CoachMessage } from '@/_services/coach-message-service';
import CoachMessageComponent from '@/_components/coach-message';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import {
    PaperAirplaneIcon,
    MagnifyingGlassIcon,
    ArrowUpIcon,
    ArrowDownIcon,
    XMarkIcon,
} from '@heroicons/react/24/outline';
import VoiceRecorder from '@/_components/VoiceRecorder';
import { useCoachMessagesSignalR } from '@/_hooks/use-coach-messages-signalr';
import { Coach, User } from '@/_lib/types';
import { ConnectionDetails } from '@/app/api/connections/[connectionId]/route';

interface MessageInterfaceProps {
    connectionId: string;
}

export default function MessageInterface({
    connectionId,
}: MessageInterfaceProps) {
    const [messages, setMessages] = useState<CoachMessage[]>([]);
    const [input, setInput] = useState('');
    const [isLoading, setIsLoading] = useState(true);
    const [isSending, setIsSending] = useState(false);
    const [isTyping, setIsTyping] = useState(false);
    const [otherUserTyping, setOtherUserTyping] = useState(false);
    const [searchTerm, setSearchTerm] = useState('');
    const [searchResults, setSearchResults] = useState<CoachMessage[]>([]);
    const [searchIndex, setSearchIndex] = useState(-1);
    const [showSearch, setShowSearch] = useState(false);
    const [hasMore, setHasMore] = useState(true);

    // Connection and user state
    const [currentUserId, setCurrentUserId] = useState<string>('');
    const [otherUserId, setOtherUserId] = useState<string>('');
    const [otherUserName, setOtherUserName] = useState<string>('Loading...');
    const [isLoadingConnection, setIsLoadingConnection] = useState(true);

    const messagesEndRef = useRef<HTMLDivElement>(null);
    const messagesContainerRef = useRef<HTMLDivElement>(null);
    const textareaRef = useRef<HTMLTextAreaElement>(null);
    const typingTimeoutRef = useRef<NodeJS.Timeout | null>(null);
    const searchTimeoutRef = useRef<NodeJS.Timeout | null>(null);

    // Handle new message from SignalR
    const handleNewMessage = useCallback(
        (message: CoachMessage) => {
            setMessages((prev) => {
                // Avoid duplicates
                if (prev.some((m) => m.id === message.id)) {
                    return prev;
                }
                return [message, ...prev].sort(
                    (a, b) =>
                        new Date(a.createdAt).getTime() -
                        new Date(b.createdAt).getTime()
                );
            });
            // Mark as delivered if we're the recipient
            if (message.senderId !== currentUserId) {
                fetchWithAuth(`/api/coach-messages/${message.id}/delivered`, {
                    method: 'PUT',
                }).catch(console.error);
            }
        },
        [currentUserId]
    );

    // Handle typing indicator from SignalR
    const handleTypingIndicator = useCallback(
        (senderId: string, typing: boolean) => {
            if (senderId !== currentUserId) {
                setOtherUserTyping(typing);
            }
        },
        [currentUserId]
    );

    // Load current user ID on mount
    useEffect(() => {
        const loadCurrentUser = async () => {
            try {
                const currentUserResponse = await fetchWithAuth('/api/user/me');
                if (currentUserResponse.ok) {
                    const currentUser: User | Coach =
                        await currentUserResponse.json();
                    const currentUserIdValue =
                        (currentUser as any).id ||
                        (currentUser as any).userProfileId ||
                        (currentUser as any).coachProfileId;
                    setCurrentUserId(currentUserIdValue);
                }
            } catch (error) {
                console.error('Error loading current user:', error);
            }
        };
        loadCurrentUser();
    }, []);

    // Load connection details and user/coach info on mount
    useEffect(() => {
        loadConnectionDetails();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [connectionId]);

    const loadConnectionDetails = async () => {
        try {
            setIsLoadingConnection(true);

            // 2. Get connection details
            const connectionResponse = await fetchWithAuth(
                `/api/connections/${connectionId}`
            );
            if (!connectionResponse.ok) {
                throw new Error('Failed to fetch connection details');
            }
            const connection: ConnectionDetails =
                await connectionResponse.json();

            // 3. Get current user ID if not already set
            let currentUserIdValue = currentUserId;
            if (!currentUserIdValue) {
                const currentUserResponse = await fetchWithAuth('/api/user/me');
                if (currentUserResponse.ok) {
                    const currentUser: User | Coach =
                        await currentUserResponse.json();
                    currentUserIdValue =
                        (currentUser as any).id ||
                        (currentUser as any).userProfileId ||
                        (currentUser as any).coachProfileId;
                    setCurrentUserId(currentUserIdValue);
                }
            }

            // 4. Determine other user ID
            const otherUserIdValue =
                currentUserIdValue === connection.userProfileId
                    ? connection.coachProfileId
                    : connection.userProfileId;
            setOtherUserId(otherUserIdValue);

            // Set userId and coachId for SignalR (always userId < coachId for consistency)
            const userIdValue =
                connection.userProfileId < connection.coachProfileId
                    ? connection.userProfileId
                    : connection.coachProfileId;
            const coachIdValue =
                connection.userProfileId < connection.coachProfileId
                    ? connection.coachProfileId
                    : connection.userProfileId;

            // 4. Fetch other user's details
            try {
                // Try fetching as coach first
                const coachResponse = await fetchWithAuth(
                    `/api/coaches/${otherUserIdValue}`
                );
                if (coachResponse.ok) {
                    const coach: Coach = await coachResponse.json();
                    setOtherUserName(coach.fullName);
                } else {
                    // If not a coach, we'll try to get name from messages later
                    // For now, set a placeholder
                    setOtherUserName('User');
                }
            } catch (error) {
                console.error('Error fetching other user details:', error);
                setOtherUserName('User');
            }
        } catch (error) {
            console.error('Error loading connection details:', error);
            setOtherUserName('Error loading user');
        } finally {
            setIsLoadingConnection(false);
        }
    };

    // Initialize SignalR connection via custom hook - only when we have connectionId
    const { sendTypingIndicator } = useCoachMessagesSignalR({
        connectionId: connectionId || '',
        onNewMessage: handleNewMessage,
        onTypingIndicator: handleTypingIndicator,
    });

    // Load initial messages - only after connection details are loaded
    useEffect(() => {
        if (!isLoadingConnection && connectionId) {
            loadMessages();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [connectionId, isLoadingConnection]);

    // Auto-scroll to bottom
    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages]);

    // Auto-resize textarea
    useEffect(() => {
        if (textareaRef.current) {
            textareaRef.current.style.height = 'auto';
            textareaRef.current.style.height =
                textareaRef.current.scrollHeight + 'px';
        }
    }, [input]);

    // Mark conversation as read when component mounts or messages change
    useEffect(() => {
        if (messages.length > 0) {
            fetchWithAuth(`/api/coach-messages/conversation/${connectionId}`, {
                method: 'PUT',
            }).catch(console.error);
        }
    }, [connectionId, messages.length]);

    const loadMessages = async (beforeDate?: Date) => {
        try {
            setIsLoading(true);
            const params = new URLSearchParams();
            params.append('pageSize', '20');
            if (beforeDate) {
                params.append('beforeDate', beforeDate.toISOString());
            }

            const response = await fetchWithAuth(
                `/api/coach-messages/conversation/${connectionId}?${params.toString()}`
            );

            if (!response.ok) {
                throw new Error('Failed to load messages');
            }

            const newMessages: CoachMessage[] = await response.json();

            // Try to get other user's name from messages if we don't have it yet
            if (otherUserName === 'User' || otherUserName === 'Loading...') {
                const otherUserMessage = newMessages.find(
                    (m) => m.senderId === otherUserId && (m as any).sender
                );
                if (otherUserMessage && (otherUserMessage as any).sender) {
                    setOtherUserName((otherUserMessage as any).sender.fullName);
                }
            }

            if (beforeDate) {
                setMessages((prev) => {
                    const combined = [...prev, ...newMessages];
                    return combined.sort(
                        (a, b) =>
                            new Date(a.createdAt).getTime() -
                            new Date(b.createdAt).getTime()
                    );
                });
            } else {
                setMessages(
                    newMessages.sort(
                        (a, b) =>
                            new Date(a.createdAt).getTime() -
                            new Date(b.createdAt).getTime()
                    )
                );
            }

            setHasMore(newMessages.length === 20);
        } catch (error) {
            console.error('Error loading messages:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const handleScroll = useCallback(() => {
        if (!messagesContainerRef.current) return;

        const container = messagesContainerRef.current;
        const isAtTop = container.scrollTop === 0;

        if (isAtTop && hasMore && !isLoading) {
            const oldestMessage = messages[0];
            if (oldestMessage) {
                loadMessages(new Date(oldestMessage.createdAt));
            }
        }
    }, [messages, hasMore, isLoading, connectionId]);

    useEffect(() => {
        const container = messagesContainerRef.current;
        if (container) {
            container.addEventListener('scroll', handleScroll);
            return () => container.removeEventListener('scroll', handleScroll);
        }
    }, [handleScroll]);

    const sendMessage = async () => {
        if (!input.trim() || isSending) return;

        setIsSending(true);
        try {
            const response = await fetchWithAuth('/api/coach-messages', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    connectionId,
                    content: input.trim(),
                }),
            });

            if (!response.ok) {
                throw new Error('Failed to send message');
            }

            const message: CoachMessage = await response.json();
            setMessages((prev) => [...prev, message]);
            setInput('');
            if (textareaRef.current) {
                textareaRef.current.style.height = 'auto';
            }
        } catch (error) {
            console.error('Error sending message:', error);
        } finally {
            setIsSending(false);
        }
    };

    const handleVoiceRecording = async (audioBlob: Blob) => {
        if (isSending) return;

        setIsSending(true);
        try {
            const formData = new FormData();
            formData.append('file', audioBlob, 'voice-message.m4a');
            formData.append('connectionId', connectionId);

            const response = await fetchWithAuth('/api/coach-messages/voice', {
                method: 'POST',
                body: formData,
            });

            if (!response.ok) {
                throw new Error('Failed to send voice message');
            }

            const message: CoachMessage = await response.json();
            setMessages((prev) => [...prev, message]);
        } catch (error) {
            console.error('Error sending voice message:', error);
        } finally {
            setIsSending(false);
        }
    };

    const handleInputChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
        setInput(e.target.value);

        // Send typing indicator
        sendTypingIndicator(true);

        // Clear existing timeout
        if (typingTimeoutRef.current) {
            clearTimeout(typingTimeoutRef.current);
        }

        // Set timeout to stop typing indicator
        typingTimeoutRef.current = setTimeout(() => {
            sendTypingIndicator(false);
        }, 1000);
    };

    const handleSearch = async (term: string) => {
        if (!term.trim()) {
            setSearchResults([]);
            setSearchIndex(-1);
            return;
        }

        try {
            const params = new URLSearchParams();
            params.append('searchTerm', term);

            const response = await fetchWithAuth(
                `/api/coach-messages/conversation/${connectionId}/search?${params.toString()}`
            );

            if (!response.ok) {
                throw new Error('Failed to search messages');
            }

            const results: CoachMessage[] = await response.json();
            setSearchResults(results);
            setSearchIndex(results.length > 0 ? 0 : -1);
        } catch (error) {
            console.error('Error searching messages:', error);
        }
    };

    const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const term = e.target.value;
        setSearchTerm(term);

        if (searchTimeoutRef.current) {
            clearTimeout(searchTimeoutRef.current);
        }

        searchTimeoutRef.current = setTimeout(() => {
            handleSearch(term);
        }, 300);
    };

    const scrollToSearchResult = (index: number) => {
        if (index < 0 || index >= searchResults.length) return;

        const messageId = searchResults[index].id;
        const element = document.getElementById(`message-${messageId}`);
        if (element) {
            element.scrollIntoView({ behavior: 'smooth', block: 'center' });
            element.classList.add('ring-2', 'ring-indigo-500');
            setTimeout(() => {
                element.classList.remove('ring-2', 'ring-indigo-500');
            }, 2000);
        }
    };

    const handleSearchNavigation = (direction: 'up' | 'down') => {
        if (searchResults.length === 0) return;

        let newIndex = searchIndex;
        if (direction === 'up') {
            newIndex =
                searchIndex <= 0 ? searchResults.length - 1 : searchIndex - 1;
        } else {
            newIndex =
                searchIndex >= searchResults.length - 1 ? 0 : searchIndex + 1;
        }

        setSearchIndex(newIndex);
        scrollToSearchResult(newIndex);
    };

    const handleKeyPress = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    };

    return (
        <div className="flex flex-col h-[calc(100vh-200px)] bg-white rounded-lg shadow-sm border border-gray-200">
            {/* Header */}
            <div className="flex items-center justify-between p-4 border-b border-gray-200">
                <div>
                    <h2 className="text-lg font-semibold text-gray-900">
                        {otherUserName}
                    </h2>
                </div>
                <div className="flex items-center space-x-2">
                    <button
                        onClick={() => setShowSearch(!showSearch)}
                        className="p-2 text-gray-600 hover:text-gray-900 hover:bg-gray-100 rounded-lg transition-colors"
                        title="Search messages"
                    >
                        <MagnifyingGlassIcon className="h-5 w-5" />
                    </button>
                </div>
            </div>

            {/* Search Bar */}
            {showSearch && (
                <div className="p-4 border-b border-gray-200 bg-gray-50">
                    <div className="flex items-center space-x-2">
                        <div className="flex-1 relative">
                            <input
                                type="text"
                                value={searchTerm}
                                onChange={handleSearchChange}
                                placeholder="Search messages..."
                                className="w-full px-4 py-2 pl-10 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                            />
                            <MagnifyingGlassIcon className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-gray-400" />
                        </div>
                        {searchResults.length > 0 && (
                            <div className="flex items-center space-x-1">
                                <button
                                    onClick={() => handleSearchNavigation('up')}
                                    className="p-2 text-gray-600 hover:text-gray-900 hover:bg-gray-200 rounded-lg transition-colors"
                                    title="Previous result"
                                >
                                    <ArrowUpIcon className="h-5 w-5" />
                                </button>
                                <span className="text-sm text-gray-600">
                                    {searchIndex + 1} / {searchResults.length}
                                </span>
                                <button
                                    onClick={() =>
                                        handleSearchNavigation('down')
                                    }
                                    className="p-2 text-gray-600 hover:text-gray-900 hover:bg-gray-200 rounded-lg transition-colors"
                                    title="Next result"
                                >
                                    <ArrowDownIcon className="h-5 w-5" />
                                </button>
                            </div>
                        )}
                        <button
                            onClick={() => {
                                setShowSearch(false);
                                setSearchTerm('');
                                setSearchResults([]);
                                setSearchIndex(-1);
                            }}
                            className="p-2 text-gray-600 hover:text-gray-900 hover:bg-gray-200 rounded-lg transition-colors"
                        >
                            <XMarkIcon className="h-5 w-5" />
                        </button>
                    </div>
                </div>
            )}

            {/* Messages Container */}
            <div
                ref={messagesContainerRef}
                className="flex-1 overflow-y-auto p-4 space-y-4"
            >
                {isLoading && messages.length === 0 ? (
                    <div className="flex justify-center items-center h-full">
                        <div className="text-gray-500">Loading messages...</div>
                    </div>
                ) : (
                    <>
                        {messages.map((message) => (
                            <div key={message.id} id={`message-${message.id}`}>
                                <CoachMessageComponent
                                    message={message}
                                    currentUserId={currentUserId}
                                    onDelete={() => {
                                        setMessages((prev) =>
                                            prev.filter(
                                                (m) => m.id !== message.id
                                            )
                                        );
                                    }}
                                />
                            </div>
                        ))}
                        {otherUserTyping && (
                            <div className="flex justify-start">
                                <div className="bg-white border border-gray-200 rounded-lg px-4 py-3">
                                    <div className="flex space-x-1">
                                        <div
                                            className="w-2 h-2 bg-gray-400 rounded-full animate-bounce"
                                            style={{ animationDelay: '0ms' }}
                                        />
                                        <div
                                            className="w-2 h-2 bg-gray-400 rounded-full animate-bounce"
                                            style={{ animationDelay: '150ms' }}
                                        />
                                        <div
                                            className="w-2 h-2 bg-gray-400 rounded-full animate-bounce"
                                            style={{ animationDelay: '300ms' }}
                                        />
                                    </div>
                                </div>
                            </div>
                        )}
                        <div ref={messagesEndRef} />
                    </>
                )}
            </div>

            {/* Input Area */}
            <div className="p-4 border-t border-gray-200">
                <div className="flex items-end space-x-2">
                    <div className="flex-1">
                        <textarea
                            ref={textareaRef}
                            value={input}
                            onChange={handleInputChange}
                            onKeyPress={handleKeyPress}
                            placeholder="Type a message..."
                            rows={1}
                            className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent resize-none"
                        />
                    </div>
                    <VoiceRecorder
                        onRecordingComplete={handleVoiceRecording}
                        disabled={isSending}
                    />
                    <button
                        onClick={sendMessage}
                        disabled={!input.trim() || isSending}
                        className="p-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                        title="Send message"
                    >
                        <PaperAirplaneIcon className="h-5 w-5" />
                    </button>
                </div>
            </div>
        </div>
    );
}
