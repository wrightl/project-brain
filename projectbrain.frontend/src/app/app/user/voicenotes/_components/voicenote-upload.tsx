'use client';

import { useState, useRef, useEffect } from 'react';
import {
    CloudArrowUpIcon,
    MicrophoneIcon,
    StopIcon,
    XMarkIcon,
    PlayIcon,
    PauseIcon,
} from '@heroicons/react/24/outline';
import { apiClient } from '@/_lib/api-client';
import { VoiceNote } from '@/_lib/types';
import { useVoiceRecording } from '@/_hooks/useVoiceRecording';

interface VoiceNoteUploadProps {
    onUploadComplete: () => void;
}

type Mode = 'upload' | 'record';

export default function VoiceNoteUpload({
    onUploadComplete,
}: VoiceNoteUploadProps) {
    const [mode, setMode] = useState<Mode>('record');
    const [file, setFile] = useState<File | null>(null);
    const [recordedBlob, setRecordedBlob] = useState<Blob | null>(null);
    const [recordedUrl, setRecordedUrl] = useState<string | null>(null);
    const [description, setDescription] = useState('');
    const [isUploading, setIsUploading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [isDragging, setIsDragging] = useState(false);
    const [isPlayingRecording, setIsPlayingRecording] = useState(false);
    const audioPreviewRef = useRef<HTMLAudioElement | null>(null);

    const {
        isRecording,
        isSupported: isRecordingSupported,
        duration: recordingDuration,
        error: recordingError,
        startRecording,
        stopRecording,
        cancelRecording,
    } = useVoiceRecording({
        onRecordingComplete: (blob) => {
            setRecordedBlob(blob);
            // Create preview URL
            const url = URL.createObjectURL(blob);
            setRecordedUrl(url);
            // Convert blob to File for upload
            const fileName = `voice-note-${Date.now()}.${getFileExtension(
                blob.type
            )}`;
            const audioFile = new File([blob], fileName, { type: blob.type });
            setFile(audioFile);
        },
        onError: (error) => {
            setError(error);
        },
    });

    // Cleanup recorded URL on unmount
    useEffect(() => {
        return () => {
            if (recordedUrl) {
                URL.revokeObjectURL(recordedUrl);
            }
        };
    }, [recordedUrl]);

    // Handle recording errors
    useEffect(() => {
        if (recordingError) {
            setError(recordingError);
        }
    }, [recordingError]);

    const getFileExtension = (mimeType: string): string => {
        if (mimeType.includes('wav')) return 'wav';
        if (mimeType.includes('webm')) return 'webm';
        if (mimeType.includes('mp4') || mimeType.includes('m4a')) return 'm4a';
        if (mimeType.includes('mpeg') || mimeType.includes('mp3')) return 'mp3';
        if (mimeType.includes('aac')) return 'aac';
        return 'webm'; // Default
    };

    const formatTime = (seconds: number): string => {
        if (isNaN(seconds) || !isFinite(seconds)) return '0:00';
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return `${mins}:${secs.toString().padStart(2, '0')}`;
    };

    const handleStartRecording = async () => {
        setError(null);
        setRecordedBlob(null);
        setRecordedUrl(null);
        setFile(null);
        if (recordedUrl) {
            URL.revokeObjectURL(recordedUrl);
            setRecordedUrl(null);
        }
        await startRecording();
    };

    const handleStopRecording = () => {
        stopRecording();
    };

    const handleCancelRecording = () => {
        cancelRecording();
        setRecordedBlob(null);
        if (recordedUrl) {
            URL.revokeObjectURL(recordedUrl);
            setRecordedUrl(null);
        }
        setFile(null);
    };

    const handlePlayPreview = () => {
        const audio = audioPreviewRef.current;
        if (!audio || !recordedUrl) return;

        if (isPlayingRecording) {
            audio.pause();
            setIsPlayingRecording(false);
        } else {
            audio.play();
            setIsPlayingRecording(true);
        }
    };

    const handleModeChange = (newMode: Mode) => {
        if (isRecording) {
            handleCancelRecording();
        }
        setMode(newMode);
        setFile(null);
        setRecordedBlob(null);
        if (recordedUrl) {
            URL.revokeObjectURL(recordedUrl);
            setRecordedUrl(null);
        }
        setError(null);
    };

    const handleDragOver = (e: React.DragEvent) => {
        e.preventDefault();
        setIsDragging(true);
    };

    const handleDragLeave = () => {
        setIsDragging(false);
    };

    const handleDrop = (e: React.DragEvent) => {
        e.preventDefault();
        setIsDragging(false);
        const droppedFiles = Array.from(e.dataTransfer.files);
        const audioFile = droppedFiles.find((f) => f.type.startsWith('audio/'));
        if (audioFile) {
            setFile(audioFile);
            setError(null);
        } else {
            setError('Please drop an audio file');
        }
    };

    const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
        const selectedFile = e.target.files?.[0];
        if (selectedFile) {
            if (selectedFile.type.startsWith('audio/')) {
                setFile(selectedFile);
                setError(null);
            } else {
                setError('Please select an audio file');
            }
        }
    };

    const handleUpload = async () => {
        if (!file) {
            setError(
                mode === 'record'
                    ? 'Please record a voice note first'
                    : 'Please select a file to upload'
            );
            return;
        }

        setIsUploading(true);
        setError(null);

        try {
            const formData = new FormData();
            formData.append('file', file);
            if (description.trim()) {
                formData.append('description', description.trim());
            }

            await apiClient<VoiceNote>('/api/user/voicenotes', {
                method: 'POST',
                body: formData,
            });

            // Reset form
            setFile(null);
            setRecordedBlob(null);
            if (recordedUrl) {
                URL.revokeObjectURL(recordedUrl);
                setRecordedUrl(null);
            }
            setDescription('');
            const fileInput = document.getElementById(
                'voice-note-upload'
            ) as HTMLInputElement;
            if (fileInput) {
                fileInput.value = '';
            }

            onUploadComplete();
        } catch (err) {
            const errorMessage =
                err instanceof Error ? err.message : 'Upload failed';
            setError(errorMessage);
            console.error('Error uploading voice note:', err);
        } finally {
            setIsUploading(false);
        }
    };

    const formatFileSize = (bytes: number) => {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return (
            Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i]
        );
    };

    return (
        <div className="bg-white shadow rounded-lg p-6 space-y-4">
            <h2 className="text-lg font-semibold text-gray-900">
                Add Voice Note
            </h2>

            {/* Mode Toggle */}
            <div className="flex border-b border-gray-200">
                <button
                    onClick={() => handleModeChange('record')}
                    className={`flex-1 px-4 py-2 text-sm font-medium transition-colors ${
                        mode === 'record'
                            ? 'text-indigo-600 border-b-2 border-indigo-600'
                            : 'text-gray-500 hover:text-gray-700'
                    }`}
                >
                    <MicrophoneIcon className="h-5 w-5 inline-block mr-2" />
                    Record
                </button>
                <button
                    onClick={() => handleModeChange('upload')}
                    className={`flex-1 px-4 py-2 text-sm font-medium transition-colors ${
                        mode === 'upload'
                            ? 'text-indigo-600 border-b-2 border-indigo-600'
                            : 'text-gray-500 hover:text-gray-700'
                    }`}
                >
                    <CloudArrowUpIcon className="h-5 w-5 inline-block mr-2" />
                    Upload
                </button>
            </div>

            {/* Recording Mode */}
            {mode === 'record' && (
                <div className="space-y-4">
                    {!isRecordingSupported && (
                        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
                            <p className="text-sm text-yellow-800">
                                Voice recording is not supported in your
                                browser. Please use the Upload tab instead.
                            </p>
                        </div>
                    )}

                    {isRecordingSupported && (
                        <>
                            {/* Recording Controls */}
                            <div className="text-center space-y-4">
                                {!isRecording && !recordedBlob && (
                                    <button
                                        onClick={handleStartRecording}
                                        disabled={isUploading}
                                        className="inline-flex items-center justify-center px-6 py-3 border border-transparent rounded-full shadow-lg text-base font-medium text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
                                    >
                                        <MicrophoneIcon className="h-6 w-6 mr-2" />
                                        Start Recording
                                    </button>
                                )}

                                {isRecording && (
                                    <div className="space-y-4">
                                        <div className="flex items-center justify-center space-x-4">
                                            <div className="flex items-center space-x-2">
                                                <div className="h-4 w-4 bg-red-600 rounded-full animate-pulse" />
                                                <span className="text-lg font-mono font-semibold text-red-600">
                                                    {formatTime(
                                                        recordingDuration
                                                    )}
                                                </span>
                                            </div>
                                        </div>
                                        <button
                                            onClick={handleStopRecording}
                                            className="inline-flex items-center justify-center px-6 py-3 border border-transparent rounded-full shadow-lg text-base font-medium text-white bg-gray-700 hover:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500 transition-colors"
                                        >
                                            <StopIcon className="h-6 w-6 mr-2" />
                                            Stop Recording
                                        </button>
                                        <button
                                            onClick={handleCancelRecording}
                                            className="block mx-auto text-sm text-gray-500 hover:text-red-600 transition-colors"
                                        >
                                            Cancel
                                        </button>
                                    </div>
                                )}

                                {/* Recorded Audio Preview */}
                                {recordedBlob && recordedUrl && (
                                    <div className="bg-gray-50 border border-gray-200 rounded-lg p-4 space-y-3">
                                        <div className="flex items-center justify-between">
                                            <p className="text-sm font-medium text-gray-900">
                                                Recording Complete
                                            </p>
                                            <button
                                                onClick={handleCancelRecording}
                                                className="text-gray-400 hover:text-red-600 transition-colors"
                                                type="button"
                                                title="Discard recording"
                                            >
                                                <XMarkIcon className="h-5 w-5" />
                                            </button>
                                        </div>
                                        <div className="flex items-center space-x-3">
                                            <button
                                                onClick={handlePlayPreview}
                                                className="flex-shrink-0 p-2 rounded-full bg-indigo-600 text-white hover:bg-indigo-700 transition-colors"
                                                type="button"
                                            >
                                                {isPlayingRecording ? (
                                                    <PauseIcon className="h-5 w-5" />
                                                ) : (
                                                    <PlayIcon className="h-5 w-5" />
                                                )}
                                            </button>
                                            <div className="flex-1 text-sm text-gray-600">
                                                <p>
                                                    Duration:{' '}
                                                    {formatTime(
                                                        recordingDuration
                                                    )}
                                                </p>
                                                <p>
                                                    Size:{' '}
                                                    {formatFileSize(
                                                        recordedBlob.size
                                                    )}
                                                </p>
                                            </div>
                                        </div>
                                        <audio
                                            ref={audioPreviewRef}
                                            src={recordedUrl}
                                            onEnded={() =>
                                                setIsPlayingRecording(false)
                                            }
                                            onPlay={() =>
                                                setIsPlayingRecording(true)
                                            }
                                            onPause={() =>
                                                setIsPlayingRecording(false)
                                            }
                                        />
                                    </div>
                                )}
                            </div>
                        </>
                    )}
                </div>
            )}

            {/* Upload Mode */}
            {mode === 'upload' && (
                <>
                    {/* Drop Zone */}
                    <div
                        onDragOver={handleDragOver}
                        onDragLeave={handleDragLeave}
                        onDrop={handleDrop}
                        className={`border-2 border-dashed rounded-lg p-8 text-center transition-colors ${
                            isDragging
                                ? 'border-indigo-500 bg-indigo-50'
                                : 'border-gray-300 hover:border-gray-400'
                        }`}
                    >
                        <CloudArrowUpIcon className="mx-auto h-12 w-12 text-gray-400" />
                        <div className="mt-4">
                            <label
                                htmlFor="voice-note-upload"
                                className="cursor-pointer text-indigo-600 hover:text-indigo-500 font-medium"
                            >
                                Choose audio file
                            </label>
                            <input
                                id="voice-note-upload"
                                type="file"
                                accept="audio/*"
                                onChange={handleFileSelect}
                                className="sr-only"
                            />
                            <span className="text-gray-600">
                                {' '}
                                or drag and drop
                            </span>
                        </div>
                        <p className="mt-2 text-xs text-gray-500">
                            M4A, MP3, AAC, WAV up to 50MB
                        </p>
                    </div>

                    {/* Selected File */}
                    {file && (
                        <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
                            <div className="flex items-center justify-between">
                                <div className="flex-1 min-w-0">
                                    <p className="text-sm font-medium text-gray-900 truncate">
                                        {file.name}
                                    </p>
                                    <p className="text-xs text-gray-500">
                                        {formatFileSize(file.size)}
                                    </p>
                                </div>
                                <button
                                    onClick={() => setFile(null)}
                                    className="ml-4 text-gray-400 hover:text-red-600"
                                    type="button"
                                >
                                    ×
                                </button>
                            </div>
                        </div>
                    )}
                </>
            )}

            {/* Description Field */}
            <div>
                <label
                    htmlFor="description"
                    className="block text-sm font-medium text-gray-700 mb-2"
                >
                    Description (optional)
                </label>
                <textarea
                    id="description"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    placeholder="Add a description for this voice note..."
                    rows={3}
                    maxLength={1000}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent resize-none"
                />
                <p className="mt-1 text-xs text-gray-500">
                    {description.length}/1000 characters
                </p>
            </div>

            {/* Error Message */}
            {error && (
                <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                    <p className="text-sm text-red-800">{error}</p>
                </div>
            )}

            {/* Upload Button */}
            <button
                onClick={handleUpload}
                disabled={!file || isUploading || isRecording}
                className="w-full inline-flex items-center justify-center px-4 py-2 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
            >
                {isUploading ? (
                    <>
                        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2" />
                        Uploading...
                    </>
                ) : mode === 'record' ? (
                    'Upload Recording'
                ) : (
                    'Upload Voice Note'
                )}
            </button>
        </div>
    );
}
