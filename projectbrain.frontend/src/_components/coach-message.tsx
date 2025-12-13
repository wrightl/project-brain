'use client';

import { useState, useRef, useEffect } from 'react';
import {
    PlayIcon,
    PauseIcon,
    TrashIcon,
    CheckIcon,
} from '@heroicons/react/24/outline';
import { CoachMessage } from '@/_services/coach-message-service';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import ConfirmationDialog from '@/_components/confirmation-dialog';

interface CoachMessageComponentProps {
    message: CoachMessage;
    currentUserId: string;
    onDelete?: () => void;
    onDeleteError?: (error: string) => void;
}

export default function CoachMessageComponent({
    message,
    currentUserId,
    onDelete,
    onDeleteError,
}: CoachMessageComponentProps) {
    const [isPlaying, setIsPlaying] = useState(false);
    const [duration, setDuration] = useState<number>(0);
    const [currentTime, setCurrentTime] = useState<number>(0);
    const [isLoading, setIsLoading] = useState(false);
    const [isDeleting, setIsDeleting] = useState(false);
    const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
    const audioRef = useRef<HTMLAudioElement | null>(null);
    const [audioSrc, setAudioSrc] = useState<string | null>(null);
    const blobUrlRef = useRef<string | null>(null);

    const isOwnMessage = message.senderId === currentUserId;

    console.log('CoachMessageComponent', message);
    console.log('currentUserId', currentUserId);
    console.log('isOwnMessage', isOwnMessage);
    console.log('message.senderId', message.senderId);

    // Load audio for voice messages
    useEffect(() => {
        if (message.messageType !== 'voice' || !message.voiceNoteUrl) {
            return;
        }

        const loadAudio = async () => {
            try {
                setIsLoading(true);
                // For voice messages, fetch the audio from our API route
                const audioUrl = `/api/coach-messages/${message.id}/audio`;
                const response = await fetchWithAuth(audioUrl);

                if (!response.ok) {
                    throw new Error(
                        `Failed to load audio: ${response.statusText}`
                    );
                }

                const blob = await response.blob();

                if (blobUrlRef.current) {
                    URL.revokeObjectURL(blobUrlRef.current);
                }

                const blobUrl = URL.createObjectURL(blob);
                blobUrlRef.current = blobUrl;
                setAudioSrc(blobUrl);
                setIsLoading(false);
            } catch (error) {
                console.error('Error loading audio:', error);
                setIsLoading(false);
            }
        };

        loadAudio();

        return () => {
            if (blobUrlRef.current) {
                URL.revokeObjectURL(blobUrlRef.current);
            }
        };
    }, [message.voiceNoteUrl, message.messageType]);

    useEffect(() => {
        const audio = audioRef.current;
        if (!audio || !audioSrc) return;

        const updateTime = () => setCurrentTime(audio.currentTime);
        const updateDuration = () => setDuration(audio.duration);
        const handleEnd = () => setIsPlaying(false);
        const handleLoadStart = () => setIsLoading(true);
        const handleCanPlay = () => setIsLoading(false);

        audio.addEventListener('timeupdate', updateTime);
        audio.addEventListener('loadedmetadata', updateDuration);
        audio.addEventListener('ended', handleEnd);
        audio.addEventListener('loadstart', handleLoadStart);
        audio.addEventListener('canplay', handleCanPlay);

        return () => {
            audio.removeEventListener('timeupdate', updateTime);
            audio.removeEventListener('loadedmetadata', updateDuration);
            audio.removeEventListener('ended', handleEnd);
            audio.removeEventListener('loadstart', handleLoadStart);
            audio.removeEventListener('canplay', handleCanPlay);
        };
    }, [audioSrc]);

    const togglePlay = () => {
        const audio = audioRef.current;
        if (!audio || !audioSrc) return;

        if (isPlaying) {
            audio.pause();
            setIsPlaying(false);
        } else {
            audio.play();
            setIsPlaying(true);
        }
    };

    const handleSeek = (e: React.ChangeEvent<HTMLInputElement>) => {
        const audio = audioRef.current;
        if (!audio) return;

        const newTime = parseFloat(e.target.value);
        audio.currentTime = newTime;
        setCurrentTime(newTime);
    };

    const handleDeleteClick = () => {
        setDeleteConfirmOpen(true);
    };

    const handleDelete = async () => {
        setIsDeleting(true);
        try {
            await fetchWithAuth(`/api/coach-messages/${message.id}`, {
                method: 'DELETE',
            });
            onDelete?.();
        } catch (err) {
            onDeleteError?.(
                err instanceof Error ? err.message : 'Unknown error'
            );
        } finally {
            setIsDeleting(false);
            setDeleteConfirmOpen(false);
        }
    };

    const formatTime = (seconds: number): string => {
        if (isNaN(seconds) || !isFinite(seconds)) return '0:00';
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return `${mins}:${secs.toString().padStart(2, '0')}`;
    };

    const formatMessageTime = (dateString: string): string => {
        try {
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
        } catch {
            return '';
        }
    };

    const getStatusIcon = () => {
        if (!isOwnMessage) return null;

        if (message.status === 'read') {
            return (
                <div className="flex items-center">
                    <CheckIcon className="h-4 w-4 text-blue-600" />
                    <CheckIcon className="h-4 w-4 text-blue-600 -ml-1" />
                </div>
            );
        } else if (message.status === 'delivered') {
            return (
                <div className="flex items-center">
                    <CheckIcon className="h-4 w-4 text-gray-400" />
                    <CheckIcon className="h-4 w-4 text-gray-400 -ml-1" />
                </div>
            );
        } else {
            return <CheckIcon className="h-4 w-4 text-gray-400" />;
        }
    };

    return (
        <div
            className={`flex ${
                isOwnMessage ? 'justify-end' : 'justify-start'
            } mb-4`}
        >
            <div
                className={`max-w-[70%] rounded-lg px-4 py-3 ${
                    isOwnMessage
                        ? 'bg-indigo-600 text-white'
                        : 'bg-white border border-gray-200 text-gray-900'
                }`}
            >
                {/* Sender name (for received messages) */}
                {!isOwnMessage && message.sender && (
                    <div className="text-xs font-medium mb-1 opacity-75">
                        {message.sender.fullName}
                    </div>
                )}

                {/* Message content */}
                {message.messageType === 'text' ? (
                    <div className="text-sm whitespace-pre-wrap break-words">
                        {message.content}
                    </div>
                ) : (
                    <div className="space-y-2">
                        {audioSrc ? (
                            <>
                                <audio
                                    ref={audioRef}
                                    src={audioSrc}
                                    preload="metadata"
                                    onLoadedMetadata={(e) => {
                                        const audio = e.currentTarget;
                                        setDuration(audio.duration);
                                    }}
                                />
                                <div className="flex items-center space-x-2">
                                    <button
                                        onClick={togglePlay}
                                        disabled={isLoading || !audioSrc}
                                        className={`flex-shrink-0 p-2 rounded-full ${
                                            isOwnMessage
                                                ? 'bg-white/20 text-white hover:bg-white/30'
                                                : 'bg-indigo-600 text-white hover:bg-indigo-700'
                                        } disabled:opacity-50 disabled:cursor-not-allowed transition-colors`}
                                        title={isPlaying ? 'Pause' : 'Play'}
                                    >
                                        {isLoading ? (
                                            <div className="h-4 w-4 border-2 border-current border-t-transparent rounded-full animate-spin" />
                                        ) : isPlaying ? (
                                            <PauseIcon className="h-4 w-4" />
                                        ) : (
                                            <PlayIcon className="h-4 w-4" />
                                        )}
                                    </button>
                                    <div className="flex-1 min-w-0">
                                        <input
                                            type="range"
                                            min="0"
                                            max={duration || 0}
                                            value={currentTime}
                                            onChange={handleSeek}
                                            className={`w-full h-1.5 rounded-lg appearance-none cursor-pointer ${
                                                isOwnMessage
                                                    ? 'bg-white/20'
                                                    : 'bg-gray-200'
                                            }`}
                                            style={{
                                                background: isOwnMessage
                                                    ? `linear-gradient(to right, rgba(255,255,255,0.5) 0%, rgba(255,255,255,0.5) ${
                                                          duration
                                                              ? (currentTime /
                                                                    duration) *
                                                                100
                                                              : 0
                                                      }%, rgba(255,255,255,0.2) ${
                                                          duration
                                                              ? (currentTime /
                                                                    duration) *
                                                                100
                                                              : 0
                                                      }%, rgba(255,255,255,0.2) 100%)`
                                                    : `linear-gradient(to right, #4f46e5 0%, #4f46e5 ${
                                                          duration
                                                              ? (currentTime /
                                                                    duration) *
                                                                100
                                                              : 0
                                                      }%, #e5e7eb ${
                                                          duration
                                                              ? (currentTime /
                                                                    duration) *
                                                                100
                                                              : 0
                                                      }%, #e5e7eb 100%)`,
                                            }}
                                        />
                                    </div>
                                    <div
                                        className={`flex-shrink-0 text-xs font-mono ${
                                            isOwnMessage
                                                ? 'text-white/80'
                                                : 'text-gray-500'
                                        }`}
                                    >
                                        {formatTime(currentTime)} /{' '}
                                        {formatTime(duration)}
                                    </div>
                                    {message.voiceNoteFileName && (
                                        <div
                                            className={`text-xs truncate max-w-[100px] ${
                                                isOwnMessage
                                                    ? 'text-white/80'
                                                    : 'text-gray-500'
                                            }`}
                                            title={message.voiceNoteFileName}
                                        >
                                            {message.voiceNoteFileName}
                                        </div>
                                    )}
                                </div>
                            </>
                        ) : (
                            <div
                                className={`text-sm ${
                                    isOwnMessage
                                        ? 'text-white/80'
                                        : 'text-gray-500'
                                }`}
                            >
                                Loading audio...
                            </div>
                        )}
                    </div>
                )}

                {/* Footer with timestamp and status */}
                <div
                    className={`flex items-center justify-between mt-2 text-xs ${
                        isOwnMessage ? 'text-white/80' : 'text-gray-500'
                    }`}
                >
                    <span>{formatMessageTime(message.createdAt)}</span>
                    <div className="flex items-center space-x-1 ml-2">
                        {getStatusIcon()}
                        {isOwnMessage && (
                            <button
                                onClick={handleDeleteClick}
                                disabled={isDeleting}
                                className={`ml-1 p-1 rounded hover:opacity-70 disabled:opacity-50 disabled:cursor-not-allowed ${
                                    isOwnMessage
                                        ? 'hover:bg-white/20'
                                        : 'hover:bg-gray-100'
                                }`}
                                title="Delete message"
                            >
                                <TrashIcon className="h-3.5 w-3.5" />
                            </button>
                        )}
                    </div>
                </div>
            </div>

            {/* Delete Confirmation Dialog */}
            <ConfirmationDialog
                isOpen={deleteConfirmOpen}
                onClose={() => setDeleteConfirmOpen(false)}
                onConfirm={handleDelete}
                title="Delete Message"
                message="Are you sure you want to delete this message?"
                confirmText="Delete"
                cancelText="Cancel"
                variant="danger"
            />
        </div>
    );
}
