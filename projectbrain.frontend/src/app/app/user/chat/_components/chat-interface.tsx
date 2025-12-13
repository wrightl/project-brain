'use client';

import { useState, useRef, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Conversation, ChatMessage, Citation } from '@/_lib/types';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { PaperAirplaneIcon, Bars3Icon } from '@heroicons/react/24/outline';
import Link from 'next/link';
import VoiceRecorder from '@/_components/VoiceRecorder';
import FeatureGate from '@/_components/feature-gate';
import ConversationsDrawer from './conversations-drawer';

interface ChatInterfaceProps {
    conversation?: Conversation;
}

export default function ChatInterface({ conversation }: ChatInterfaceProps) {
    const router = useRouter();
    const [messages, setMessages] = useState<ChatMessage[]>(
        conversation?.messages || []
    );
    const [conversationId, setConversationId] = useState<string | undefined>(
        conversation?.id
    );
    const [input, setInput] = useState('');
    const [isStreaming, setIsStreaming] = useState(false);
    const [streamingMessage, setStreamingMessage] = useState('');
    const [transcribedText, setTranscribedText] = useState('');
    const [isProcessingVoice, setIsProcessingVoice] = useState(false);
    const [isDrawerOpen, setIsDrawerOpen] = useState(false);
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const textareaRef = useRef<HTMLTextAreaElement>(null);
    const streamingMessageRef = useRef<string>('');

    // Auto-scroll to bottom when messages change
    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, streamingMessage]);

    // Auto-resize textarea
    useEffect(() => {
        if (textareaRef.current) {
            textareaRef.current.style.height = 'auto';
            textareaRef.current.style.height =
                textareaRef.current.scrollHeight + 'px';
        }
    }, [input]);

    // Sync conversationId and messages when conversation prop changes
    useEffect(() => {
        if (conversation) {
            setConversationId(conversation.id);
            // Update messages from server when conversation prop changes
            // This ensures we show previous messages when resuming a conversation
            setMessages(conversation.messages || []);
        } else {
            // If conversation is undefined, reset to empty
            setConversationId(undefined);
            setMessages([]);
        }
    }, [conversation]);

    const handleVoiceRecording = async (audioBlob: Blob) => {
        if (isStreaming || isProcessingVoice) return;

        setIsProcessingVoice(true);
        setIsStreaming(true);
        setStreamingMessage('');
        setTranscribedText('');

        try {
            streamingMessageRef.current = '';

            // Create FormData for voice chat
            const formData = new FormData();
            formData.append('audio', audioBlob, 'audio.wav');
            if (conversationId) {
                formData.append('conversationId', conversationId);
            }

            // Call API route for streaming voice chat
            const response = await fetchWithAuth('/api/chat/voice', {
                method: 'POST',
                body: formData,
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || 'Failed to stream voice chat');
            }

            // Get conversation ID from response header
            const newConversationId = response.headers.get('X-Conversation-Id');
            if (newConversationId) {
                setConversationId(newConversationId);
                if (
                    newConversationId &&
                    (!conversationId || newConversationId !== conversationId)
                ) {
                    router.push(`/app/user/chat/${newConversationId}`);
                }
            }

            // Stream response using ReadableStream
            const reader = response.body?.getReader();
            const decoder = new TextDecoder();
            if (!reader) throw new Error('No response body');

            let citations: Citation[] = [];
            let done = false;
            while (!done) {
                const { value, done: streamDone } = await reader.read();
                done = streamDone;
                if (value) {
                    const text = decoder.decode(value, { stream: true });
                    const lines = text.split('\n');
                    for (const line of lines) {
                        if (line.startsWith('data: ')) {
                            const data = line.slice(6);
                            try {
                                const parsed = JSON.parse(data);
                                if (
                                    parsed.type === 'citations' &&
                                    parsed.value
                                ) {
                                    // Handle citations metadata
                                    citations = parsed.value;
                                } else if (
                                    parsed.value &&
                                    parsed.type !== 'citations'
                                ) {
                                    // Handle text chunks
                                    streamingMessageRef.current += parsed.value;
                                    setStreamingMessage(
                                        streamingMessageRef.current
                                    );
                                }
                            } catch {
                                // Ignore parse errors
                            }
                        }
                    }
                }
            }

            // Add complete assistant message
            if (streamingMessageRef.current) {
                const assistantMessage: ChatMessage = {
                    role: 'assistant',
                    content: streamingMessageRef.current,
                    citations: citations.length > 0 ? citations : undefined,
                };
                setMessages((prev) => [...prev, assistantMessage]);
            }
        } catch (error) {
            console.error('Voice chat error:', error);
            // Show error message
            const errorMessage: ChatMessage = {
                role: 'assistant',
                content:
                    'Sorry, I encountered an error processing your voice message. Please try again.',
            };
            setMessages((prev) => [...prev, errorMessage]);
        } finally {
            setIsProcessingVoice(false);
            setIsStreaming(false);
            setStreamingMessage('');
            setTranscribedText('');
            router.refresh();
        }
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!input.trim() || isStreaming) return;

        const userMessage: ChatMessage = {
            role: 'user',
            content: input.trim(),
        };

        // Add user message immediately
        setMessages((prev) => [...prev, userMessage]);
        setInput('');
        setIsStreaming(true);
        setStreamingMessage('');

        try {
            streamingMessageRef.current = '';

            // Call API route for streaming chat
            const response = await fetchWithAuth('/api/chat/stream', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    content: userMessage.content,
                    conversationId,
                }),
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || 'Failed to stream chat');
            }

            // Get conversation ID from response header
            const newConversationId = response.headers.get('X-Conversation-Id');
            if (newConversationId) {
                setConversationId(newConversationId);
                // if (
                //     newConversationId &&
                //     (!conversationId || newConversationId !== conversationId)
                // ) {
                //     router.push(`/app/user/chat/${newConversationId}`);
                // }
            }

            // Stream response using ReadableStream
            const reader = response.body?.getReader();
            const decoder = new TextDecoder();
            if (!reader) throw new Error('No response body');

            let citations: Citation[] = [];
            let done = false;
            while (!done) {
                const { value, done: streamDone } = await reader.read();
                done = streamDone;
                if (value) {
                    const text = decoder.decode(value, { stream: true });
                    // Assume SSE format: lines starting with 'data: '
                    const lines = text.split('\n');
                    for (const line of lines) {
                        if (line.startsWith('data: ')) {
                            const data = line.slice(6);
                            try {
                                const parsed = JSON.parse(data);
                                if (
                                    parsed.type === 'citations' &&
                                    parsed.value
                                ) {
                                    // Handle citations metadata
                                    citations = parsed.value;
                                } else if (
                                    parsed.value &&
                                    parsed.type !== 'citations'
                                ) {
                                    // Handle text chunks
                                    streamingMessageRef.current += parsed.value;
                                    setStreamingMessage(
                                        streamingMessageRef.current
                                    );
                                }
                            } catch {
                                // Ignore parse errors
                            }
                        }
                    }
                }
            }

            // Add complete assistant message
            if (streamingMessageRef.current) {
                const assistantMessage: ChatMessage = {
                    role: 'assistant',
                    content: streamingMessageRef.current,
                    citations: citations.length > 0 ? citations : undefined,
                };
                setMessages((prev) => [...prev, assistantMessage]);
            }
        } catch (error) {
            console.error('Chat error:', error);
            // Show error message
            const errorMessage: ChatMessage = {
                role: 'assistant',
                content: 'Sorry, I encountered an error. Please try again.',
            };
            setMessages((prev) => [...prev, errorMessage]);
        } finally {
            setIsStreaming(false);
            setStreamingMessage('');
            router.refresh();
        }
    };

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSubmit(e as unknown as React.FormEvent);
        }
    };

    const handleConversationSelect = (conversationId: string) => {
        router.push(`/app/user/chat/${conversationId}`);
    };

    // Function to render message content with clickable citations
    const renderMessageWithCitations = (
        content: string,
        citations?: Citation[]
    ) => {
        if (!citations || citations.length === 0) {
            return content;
        }

        // Pattern to match citations like [1], [2], etc.
        const citationPattern = /\[(\d+)\]/g;
        const parts: (string | React.ReactNode)[] = [];
        let lastIndex = 0;
        let match;
        let keyCounter = 0;

        while ((match = citationPattern.exec(content)) !== null) {
            // Add text before the citation
            if (match.index > lastIndex) {
                parts.push(content.substring(lastIndex, match.index));
            }

            // Add the citation link
            const citationNum = parseInt(match[1], 10);
            const citation = citations.find((c) => c.index === citationNum);

            if (citation && citation.storageUrl) {
                parts.push(
                    <Link
                        key={`citation-${keyCounter++}`}
                        href={
                            citation.isShared
                                ? `/api/admin/resources/file/${citation.id}`
                                : `/api/user/resources/file/${citation.id}`
                        }
                        target="_blank"
                        rel="noopener noreferrer"
                        className="text-blue-600 hover:text-blue-800 underline font-medium"
                        title={`${citation.sourceFile}${
                            citation.sourcePage
                                ? ` - ${citation.sourcePage}`
                                : ''
                        }`}
                    >
                        [{citationNum}]
                    </Link>
                );
            } else {
                // If no citation metadata, just render as text
                parts.push(`[${citationNum}]`);
            }

            lastIndex = match.index + match[0].length;
        }

        // Add remaining text
        if (lastIndex < content.length) {
            parts.push(content.substring(lastIndex));
        }

        return parts.length > 0 ? <>{parts}</> : content;
    };

    return (
        <div className="h-full flex flex-col relative">
            {/* Header with drawer toggle */}
            <div className="flex-shrink-0 bg-white border-b border-gray-200 px-4 py-2">
                <div className="flex items-center justify-between w-full">
                    <button
                        onClick={() => setIsDrawerOpen(true)}
                        className="p-2 text-gray-400 hover:text-gray-600 transition-colors"
                        aria-label="Open conversations"
                    >
                        <Bars3Icon className="h-6 w-6" />
                    </button>
                    <div className="flex-1 text-center">
                        <h1 className="text-lg font-semibold text-gray-900">
                            {conversation?.title || 'Chat Assistant'}
                        </h1>
                    </div>
                    <div className="w-10" />
                </div>
            </div>

            {/* Messages */}
            <div className="flex-1 overflow-y-auto px-4 py-4 min-h-0">
                <div className="w-full space-y-6">
                    {messages.length === 0 && (
                        <div className="text-center py-12">
                            <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-indigo-100 mb-4">
                                <svg
                                    className="w-8 h-8 text-indigo-600"
                                    fill="none"
                                    stroke="currentColor"
                                    viewBox="0 0 24 24"
                                >
                                    <path
                                        strokeLinecap="round"
                                        strokeLinejoin="round"
                                        strokeWidth={2}
                                        d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z"
                                    />
                                </svg>
                            </div>
                            <h3 className="text-lg font-medium text-gray-900 mb-2">
                                Start a conversation
                            </h3>
                            <p className="text-sm text-gray-500">
                                Ask me anything! I&apos;m here to help.
                            </p>
                        </div>
                    )}

                    {messages.map((message, index) => (
                        <div
                            key={index}
                            className={`flex ${
                                message.role === 'user'
                                    ? 'justify-end'
                                    : 'justify-start'
                            }`}
                        >
                            <div
                                className={`max-w-4xl rounded-lg px-4 py-3 ${
                                    message.role === 'user'
                                        ? 'bg-indigo-600 text-white'
                                        : 'bg-white border border-gray-200 text-gray-900'
                                }`}
                            >
                                <div className="text-xs font-medium mb-1 opacity-75">
                                    {message.role === 'user'
                                        ? 'You'
                                        : 'Assistant'}
                                </div>
                                <div className="text-sm whitespace-pre-wrap">
                                    {message.role === 'assistant' &&
                                    message.citations
                                        ? renderMessageWithCitations(
                                              message.content,
                                              message.citations
                                          )
                                        : message.content}
                                </div>
                            </div>
                        </div>
                    ))}

                    {/* Transcribed text preview (during voice processing) */}
                    {isProcessingVoice && transcribedText && (
                        <div className="flex justify-end">
                            <div className="max-w-4xl rounded-lg px-4 py-3 bg-indigo-100 border border-indigo-200 text-indigo-900">
                                <div className="text-xs font-medium mb-1 opacity-75">
                                    Transcribed from voice
                                </div>
                                <div className="text-sm whitespace-pre-wrap">
                                    {transcribedText}
                                </div>
                            </div>
                        </div>
                    )}

                    {/* Streaming message */}
                    {isStreaming && streamingMessage && (
                        <div className="flex justify-start">
                            <div className="max-w-4xl rounded-lg px-4 py-3 bg-white border border-gray-200 text-gray-900">
                                <div className="text-xs font-medium mb-1 opacity-75">
                                    Assistant
                                </div>
                                <div className="text-sm whitespace-pre-wrap">
                                    {streamingMessage}
                                    <span className="inline-block w-2 h-4 ml-1 bg-gray-400 animate-pulse" />
                                </div>
                            </div>
                        </div>
                    )}

                    {/* Loading indicator */}
                    {isStreaming && !streamingMessage && (
                        <div className="flex justify-start">
                            <div className="max-w-4xl rounded-lg px-4 py-3 bg-white border border-gray-200">
                                <div className="text-xs font-medium mb-1 opacity-75">
                                    {isProcessingVoice
                                        ? 'Processing voice...'
                                        : 'Assistant'}
                                </div>
                                <div className="flex space-x-2">
                                    <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" />
                                    <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce delay-75" />
                                    <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce delay-150" />
                                </div>
                            </div>
                        </div>
                    )}

                    <div ref={messagesEndRef} />
                </div>
            </div>

            {/* Input */}
            <div className="flex-shrink-0 bg-white border-t border-gray-200 px-4 py-3">
                <form onSubmit={handleSubmit} className="w-full">
                    <div className="flex items-end space-x-3">
                        <div className="flex-1">
                            <textarea
                                ref={textareaRef}
                                value={input}
                                onChange={(e) => setInput(e.target.value)}
                                onKeyDown={handleKeyDown}
                                placeholder="Type or speak your message... (Press Enter to send, Shift+Enter for new line)"
                                disabled={isStreaming}
                                rows={1}
                                className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent resize-none disabled:bg-gray-100 disabled:cursor-not-allowed"
                                style={{ maxHeight: '200px' }}
                            />
                        </div>
                        <FeatureGate
                            feature="speech_input"
                            showUpgradePrompt={false}
                        >
                            <VoiceRecorder
                                onRecordingComplete={handleVoiceRecording}
                                onError={(error) =>
                                    console.error(
                                        'Voice recording error:',
                                        error
                                    )
                                }
                                disabled={isStreaming}
                            />
                        </FeatureGate>
                        <button
                            type="submit"
                            disabled={!input.trim() || isStreaming}
                            className="flex-shrink-0 inline-flex items-center justify-center w-10 h-10 rounded-lg bg-indigo-600 text-white hover:bg-indigo-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
                        >
                            <PaperAirplaneIcon className="h-5 w-5" />
                        </button>
                    </div>
                    <p className="mt-2 text-xs text-gray-500">
                        Press Enter to send, Shift+Enter for a new line, or
                        click the microphone to record voice
                    </p>
                </form>
            </div>

            {/* Conversations Drawer */}
            <ConversationsDrawer
                isOpen={isDrawerOpen}
                onClose={() => setIsDrawerOpen(false)}
                currentConversationId={conversationId}
                onConversationSelect={handleConversationSelect}
            />
        </div>
    );
}
