'use client';

import { useState, useEffect } from 'react';
import AudioPlayer from './audio-player';
import VoiceNoteUpload from './voicenote-upload';
import { VoiceNote, PagedResponse } from '@/_lib/types';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';

export default function VoiceNotesList() {
    const [voiceNotes, setVoiceNotes] = useState<VoiceNote[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const fetchVoiceNotes = async () => {
        setIsLoading(true);
        setError(null);

        try {
            const response = await fetchWithAuth('/api/user/voicenotes');
            if (!response.ok) {
                throw new Error('Failed to fetch voice notes');
            }
            const data = (await response.json()) as PagedResponse<VoiceNote>;
            // Extract items from paginated response
            setVoiceNotes(data.items || []);
            setIsLoading(false);
        } catch (err) {
            setError(
                err instanceof Error ? err.message : 'Unknown error occurred'
            );
            setIsLoading(false);
        }
    };

    useEffect(() => {
        fetchVoiceNotes();
    }, []);

    const handleDelete = () => {
        // Refresh the list after deletion
        fetchVoiceNotes();
    };

    const handleDeleteError = (errorMessage: string) => {
        setError(errorMessage);
        // Clear error after a few seconds
        setTimeout(() => setError(null), 5000);
    };

    if (isLoading) {
        return (
            <div className="flex justify-center items-center py-12">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600" />
            </div>
        );
    }

    if (error && voiceNotes.length === 0) {
        return (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                <p className="text-sm text-red-800">{error}</p>
                <button
                    onClick={fetchVoiceNotes}
                    className="mt-2 text-sm text-red-600 hover:text-red-800 underline"
                >
                    Try again
                </button>
            </div>
        );
    }

    if (voiceNotes.length === 0) {
        return (
            <div className="text-center py-12">
                <p className="text-gray-500 text-lg">No voice notes found</p>
                <p className="text-gray-400 text-sm mt-2">
                    Record a voice note to see it here
                </p>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            <VoiceNoteUpload onUploadComplete={fetchVoiceNotes} />

            {error && (
                <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
                    <p className="text-sm text-yellow-800">{error}</p>
                </div>
            )}

            <div className="space-y-4">
                {voiceNotes.map((voiceNote) => (
                    <AudioPlayer
                        key={voiceNote.id}
                        voiceNoteId={voiceNote.id}
                        fileName={voiceNote.fileName}
                        description={voiceNote.description}
                        createdAt={voiceNote.createdAt}
                        onDelete={handleDelete}
                        onDeleteError={handleDeleteError}
                    />
                ))}
            </div>
        </div>
    );
}
