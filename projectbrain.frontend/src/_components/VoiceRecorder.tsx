'use client';

import { MicrophoneIcon, StopIcon, XMarkIcon } from '@heroicons/react/24/solid';
import { useVoiceRecording } from '@/_hooks/useVoiceRecording';

interface VoiceRecorderProps {
    onRecordingComplete: (audioBlob: Blob) => void;
    onError?: (error: string) => void;
    disabled?: boolean;
    className?: string;
}

export default function VoiceRecorder({
    onRecordingComplete,
    onError,
    disabled = false,
    className = '',
}: VoiceRecorderProps) {
    const {
        isRecording,
        isSupported,
        duration,
        error,
        startRecording,
        stopRecording,
        cancelRecording,
    } = useVoiceRecording({
        onRecordingComplete,
        onError,
    });

    if (!isSupported) {
        return (
            <div className={`text-gray-400 text-xs ${className}`}>
                Voice recording not supported
            </div>
        );
    }

    const formatDuration = (seconds: number): string => {
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return `${mins}:${secs.toString().padStart(2, '0')}`;
    };

    if (isRecording) {
        return (
            <div className={`flex items-center space-x-2 ${className}`}>
                <div className="flex items-center space-x-2 bg-red-50 text-red-700 px-3 py-2 rounded-lg border border-red-200">
                    <div className="w-2 h-2 bg-red-500 rounded-full animate-pulse" />
                    <span className="text-sm font-medium">
                        {formatDuration(duration)}
                    </span>
                </div>
                <button
                    type="button"
                    onClick={stopRecording}
                    className="flex-shrink-0 inline-flex items-center justify-center w-10 h-10 rounded-lg bg-red-600 text-white hover:bg-red-700 transition-colors"
                    title="Stop recording"
                >
                    <StopIcon className="h-5 w-5" />
                </button>
                <button
                    type="button"
                    onClick={cancelRecording}
                    className="flex-shrink-0 inline-flex items-center justify-center w-8 h-8 rounded-lg bg-gray-300 text-gray-600 hover:bg-gray-400 transition-colors"
                    title="Cancel recording"
                >
                    <XMarkIcon className="h-4 w-4" />
                </button>
            </div>
        );
    }

    return (
        <button
            type="button"
            onClick={startRecording}
            disabled={disabled}
            className={`flex-shrink-0 inline-flex items-center justify-center w-10 h-10 rounded-lg bg-gray-100 text-gray-600 hover:bg-gray-200 disabled:bg-gray-50 disabled:text-gray-400 disabled:cursor-not-allowed transition-colors ${className}`}
            title="Record voice message"
        >
            <MicrophoneIcon className="h-5 w-5" />
        </button>
    );
}
