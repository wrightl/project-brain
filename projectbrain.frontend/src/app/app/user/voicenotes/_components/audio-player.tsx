'use client';

import { useState, useRef, useEffect } from 'react';
import {
    PlayIcon,
    PauseIcon,
    SpeakerWaveIcon,
    TrashIcon,
} from '@heroicons/react/24/outline';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';

interface AudioPlayerProps {
    voiceNoteId: string;
    fileName: string;
    description?: string | null;
    createdAt?: string;
    onDelete?: () => void;
    onDeleteError?: (error: string) => void;
}

export default function AudioPlayer({
    voiceNoteId,
    fileName,
    description,
    createdAt,
    onDelete,
    onDeleteError,
}: AudioPlayerProps) {
    const [isPlaying, setIsPlaying] = useState(false);
    const [duration, setDuration] = useState<number>(0);
    const [currentTime, setCurrentTime] = useState<number>(0);
    const [isLoading, setIsLoading] = useState(false);
    const [isDeleting, setIsDeleting] = useState(false);
    const audioRef = useRef<HTMLAudioElement | null>(null);
    const [audioSrc, setAudioSrc] = useState<string | null>(null);
    const blobUrlRef = useRef<string | null>(null);

    // Fetch audio using API route and create blob URL
    useEffect(() => {
        const loadAudio = async () => {
            try {
                setIsLoading(true);

                // Use API route with fetchWithAuth
                const response = await fetchWithAuth(
                    `/api/user/voicenotes/${voiceNoteId}/audio`
                );

                if (!response.ok) {
                    throw new Error(
                        `Failed to load audio: ${response.statusText}`
                    );
                }

                // Create blob from response
                const blob = await response.blob();

                // Clean up old blob URL if exists
                if (blobUrlRef.current) {
                    URL.revokeObjectURL(blobUrlRef.current);
                }

                // Create new blob URL
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

        // Cleanup blob URL on unmount
        return () => {
            if (blobUrlRef.current) {
                URL.revokeObjectURL(blobUrlRef.current);
            }
        };
    }, [voiceNoteId]);

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

    const handleDelete = async () => {
        if (!confirm('Are you sure you want to delete this voice note?')) {
            return;
        }

        setIsDeleting(true);
        try {
            await fetchWithAuth(`/api/user/voicenotes/${voiceNoteId}`, {
                method: 'DELETE',
            });
            onDelete?.();
        } catch (err) {
            onDeleteError?.(
                err instanceof Error ? err.message : 'Unknown error'
            );
        } finally {
            setIsDeleting(false);
        }
    };

    const formatTime = (seconds: number): string => {
        if (isNaN(seconds) || !isFinite(seconds)) return '0:00';
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return `${mins}:${secs.toString().padStart(2, '0')}`;
    };

    const formatDate = (dateString?: string): string => {
        if (!dateString) return '';
        try {
            const date = new Date(dateString);
            return date.toLocaleDateString('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit',
            });
        } catch {
            return '';
        }
    };

    return (
        <div className="bg-white border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow">
            <div className="flex items-center justify-between mb-3">
                <div className="flex items-center space-x-3 flex-1 min-w-0">
                    <SpeakerWaveIcon className="h-5 w-5 text-indigo-600 flex-shrink-0" />
                    <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-gray-900 truncate">
                            {description ? description : fileName}
                        </p>
                        {description && (
                            <p className="text-xs text-gray-600 mt-1 line-clamp-2">
                                {fileName}
                            </p>
                        )}
                        {createdAt && (
                            <p className="text-xs text-gray-500">
                                {formatDate(createdAt)}
                            </p>
                        )}
                    </div>
                </div>
                <button
                    onClick={handleDelete}
                    disabled={isDeleting}
                    className="ml-2 p-2 text-gray-400 hover:text-red-600 disabled:opacity-50 disabled:cursor-not-allowed"
                    title="Delete voice note"
                >
                    <TrashIcon className="h-5 w-5" />
                </button>
            </div>

            {audioSrc && (
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

                    <div className="flex items-center space-x-3">
                        <button
                            onClick={togglePlay}
                            disabled={isLoading || !audioSrc}
                            className="flex-shrink-0 p-2 rounded-full bg-indigo-600 text-white hover:bg-indigo-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
                            title={isPlaying ? 'Pause' : 'Play'}
                        >
                            {isLoading ? (
                                <div className="h-5 w-5 border-2 border-white border-t-transparent rounded-full animate-spin" />
                            ) : isPlaying ? (
                                <PauseIcon className="h-5 w-5" />
                            ) : (
                                <PlayIcon className="h-5 w-5" />
                            )}
                        </button>

                        <div className="flex-1">
                            <input
                                type="range"
                                min="0"
                                max={duration || 0}
                                value={currentTime}
                                onChange={handleSeek}
                                className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer slider"
                                style={{
                                    background: `linear-gradient(to right, #4f46e5 0%, #4f46e5 ${
                                        duration
                                            ? (currentTime / duration) * 100
                                            : 0
                                    }%, #e5e7eb ${
                                        duration
                                            ? (currentTime / duration) * 100
                                            : 0
                                    }%, #e5e7eb 100%)`,
                                }}
                            />
                        </div>

                        <div className="flex-shrink-0 text-xs text-gray-500 font-mono">
                            {formatTime(currentTime)} / {formatTime(duration)}
                        </div>
                    </div>
                </>
            )}

            {!audioSrc && (
                <div className="text-sm text-gray-500">Loading audio...</div>
            )}
        </div>
    );
}
