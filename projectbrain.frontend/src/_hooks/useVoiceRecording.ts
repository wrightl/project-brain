'use client';

import { useState, useRef, useCallback } from 'react';

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
        isSupported:
            typeof window !== 'undefined' && 'mediaDevices' in navigator,
        duration: 0,
        error: null,
    });

    const mediaRecorderRef = useRef<MediaRecorder | null>(null);
    const chunksRef = useRef<Blob[]>([]);
    const timerRef = useRef<NodeJS.Timeout | null>(null);
    const streamRef = useRef<MediaStream | null>(null);

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

            const mediaRecorder = new MediaRecorder(stream, {
                mimeType: 'audio/webm;codecs=opus',
            });

            mediaRecorderRef.current = mediaRecorder;

            mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    chunksRef.current.push(event.data);
                }
            };

            mediaRecorder.onstop = () => {
                const audioBlob = new Blob(chunksRef.current, {
                    type: 'audio/webm;codecs=opus',
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
