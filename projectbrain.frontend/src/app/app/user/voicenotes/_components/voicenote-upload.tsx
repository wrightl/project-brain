'use client';

import { useState } from 'react';
import { CloudArrowUpIcon } from '@heroicons/react/24/outline';
import { apiClient } from '@/_lib/api-client';
import { VoiceNote } from '@/_lib/types';

interface VoiceNoteUploadProps {
    onUploadComplete: () => void;
}

export default function VoiceNoteUpload({
    onUploadComplete,
}: VoiceNoteUploadProps) {
    const [file, setFile] = useState<File | null>(null);
    const [description, setDescription] = useState('');
    const [isUploading, setIsUploading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [isDragging, setIsDragging] = useState(false);

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
        const audioFile = droppedFiles.find((f) =>
            f.type.startsWith('audio/')
        );
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
            setError('Please select a file to upload');
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
                Upload Voice Note
            </h2>

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
                    <span className="text-gray-600"> or drag and drop</span>
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
                disabled={!file || isUploading}
                className="w-full inline-flex items-center justify-center px-4 py-2 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
            >
                {isUploading ? (
                    <>
                        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2" />
                        Uploading...
                    </>
                ) : (
                    'Upload Voice Note'
                )}
            </button>
        </div>
    );
}

