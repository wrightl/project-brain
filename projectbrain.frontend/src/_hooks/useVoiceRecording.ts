'use client';

import { useState, useRef, useCallback, useEffect } from 'react';

interface VoiceRecordingOptions {
    onRecordingComplete?: (audioBlob: Blob) => void;
    onError?: (error: string) => void;
}

export interface VoiceRecordingState {
    isRecording: boolean;
    isSupported: boolean;
    duration: number;
    error: string | null;
}

export function useVoiceRecording(options: VoiceRecordingOptions = {}) {
    const [state, setState] = useState<VoiceRecordingState>({
        isRecording: false,
        isSupported: false, // Initialize to false for consistent SSR
        duration: 0,
        error: null,
    });

    // Check for support only on client after mount to avoid hydration mismatch
    useEffect(() => {
        const checkSupport = () => {
            const supported =
                typeof window !== 'undefined' &&
                'mediaDevices' in navigator &&
                'MediaRecorder' in window;
            setState((prev) => ({ ...prev, isSupported: supported }));
        };
        checkSupport();
    }, []);

    const mediaRecorderRef = useRef<MediaRecorder | null>(null);
    const chunksRef = useRef<Blob[]>([]);
    const timerRef = useRef<NodeJS.Timeout | null>(null);
    const streamRef = useRef<MediaStream | null>(null);
    const mimeTypeRef = useRef<string>('audio/webm;codecs=opus');

    const startRecording = useCallback(async () => {
        if (!state.isSupported) {
            const error = 'Voice recording is not supported in this browser';
            setState((prev) => ({ ...prev, error }));
            options.onError?.(error);
            return;
        }

        try {
            console.log('Starting recording');
            const stream = await navigator.mediaDevices.getUserMedia({
                audio: {
                    echoCancellation: true,
                    noiseSuppression: true,
                    autoGainControl: true,
                },
            });

            streamRef.current = stream;
            chunksRef.current = [];

            // Try to find a supported format that's compatible with the backend
            // Backend accepts: audio/m4a, audio/mpeg, audio/aac, audio/wav, audio/x-m4a, audio/mp4
            // Try WAV first (well-supported), then WebM (fallback)
            const supportedMimeTypes = [
                'audio/wav',
                'audio/webm;codecs=opus',
                'audio/webm',
                'audio/mp4',
                'audio/mpeg',
            ];

            let selectedMimeType = 'audio/webm;codecs=opus'; // Default fallback
            for (const mimeType of supportedMimeTypes) {
                if (MediaRecorder.isTypeSupported(mimeType)) {
                    selectedMimeType = mimeType;
                    break;
                }
            }

            mimeTypeRef.current = selectedMimeType;

            const mediaRecorder = new MediaRecorder(stream, {
                mimeType: selectedMimeType,
            });

            mediaRecorderRef.current = mediaRecorder;

            mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    chunksRef.current.push(event.data);
                }
            };

            mediaRecorder.onstop = () => {
                // Use the same mime type that was used for recording
                const audioBlob = new Blob(chunksRef.current, {
                    type: mimeTypeRef.current,
                });
                options.onRecordingComplete?.(audioBlob);

                // Clean up stream
                if (streamRef.current) {
                    streamRef.current
                        .getTracks()
                        .forEach((track) => track.stop());
                    streamRef.current = null;
                }
            };

            mediaRecorder.start(100); // Collect data every 100ms

            setState((prev) => ({
                ...prev,
                isRecording: true,
                error: null,
                duration: 0,
            }));

            // Start timer
            timerRef.current = setInterval(() => {
                setState((prev) => ({
                    ...prev,
                    duration: prev.duration + 0.1,
                }));
            }, 100);
        } catch (error) {
            const errorMessage =
                error instanceof Error
                    ? error.message
                    : 'Failed to start recording';
            setState((prev) => ({ ...prev, error: errorMessage }));
            options.onError?.(errorMessage);
        }
    }, [state.isSupported, options]);

    const stopRecording = useCallback(() => {
        if (mediaRecorderRef.current && state.isRecording) {
            mediaRecorderRef.current.stop();
            setState((prev) => ({ ...prev, isRecording: false }));

            // Clear timer
            if (timerRef.current) {
                clearInterval(timerRef.current);
                timerRef.current = null;
            }
        }
    }, [state.isRecording]);

    const cancelRecording = useCallback(() => {
        if (mediaRecorderRef.current) {
            mediaRecorderRef.current.stop();
            setState((prev) => ({ ...prev, isRecording: false, duration: 0 }));

            // Clear timer
            if (timerRef.current) {
                clearInterval(timerRef.current);
                timerRef.current = null;
            }

            // Clean up stream without calling onRecordingComplete
            if (streamRef.current) {
                streamRef.current.getTracks().forEach((track) => track.stop());
                streamRef.current = null;
            }
        }
    }, []);

    return {
        ...state,
        startRecording,
        stopRecording,
        cancelRecording,
    };
}
