'use client';

import { useState, useEffect } from 'react';
import {
    CloudArrowUpIcon,
    XMarkIcon,
    CheckCircleIcon,
    ArrowPathIcon,
} from '@heroicons/react/24/outline';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { apiClient } from '@/_lib/api-client';
import { Subscription, Usage } from '@/_lib/types';
import Link from 'next/link';

interface UploadedFile {
    file: File;
    status: 'pending' | 'uploading' | 'success' | 'error';
    message?: string;
    chunks?: number;
    errorType?: 'network' | 'limit' | 'other';
}

export default function FileUploadForm({
    onUploadComplete,
    manageSharedFiles,
}: {
    manageSharedFiles: boolean;
    onUploadComplete: () => void;
}) {
    const [files, setFiles] = useState<UploadedFile[]>([]);
    const [isDragging, setIsDragging] = useState(false);
    const [isUploading, setIsUploading] = useState(false);
    const [subscription, setSubscription] = useState<Subscription | null>(null);
    const [usage, setUsage] = useState<Usage | null>(null);
    const [limitError, setLimitError] = useState<string | null>(null);

    // Load subscription and usage data
    useEffect(() => {
        if (!manageSharedFiles) {
            loadSubscriptionData();
        }
    }, [manageSharedFiles]);

    // Clear files list when all files have status 'success'
    useEffect(() => {
        if (files.length > 0 && files.every((f) => f.status === 'success')) {
            // Clear after a short delay to show success state briefly
            const timer = setTimeout(() => {
                setFiles([]);
            }, 1000);
            return () => clearTimeout(timer);
        }
    }, [files]);

    const loadSubscriptionData = async () => {
        try {
            const [subData, usageData] = await Promise.all([
                apiClient<Subscription>('/api/subscriptions/me'),
                apiClient<Usage>('/api/subscriptions/usage'),
            ]);
            setSubscription(subData);
            setUsage(usageData);
        } catch (err) {
            console.error('Failed to load subscription data:', err);
        }
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
        addFiles(droppedFiles);
    };

    const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
        const selectedFiles = Array.from(e.target.files || []);
        addFiles(selectedFiles);
    };

    const getLimits = (tier: string) => {
        if (tier === 'Free') {
            return { maxFiles: 20, maxFileStorageMB: 100 };
        } else if (tier === 'Pro') {
            return { maxFiles: -1, maxFileStorageMB: 500 };
        } else {
            return { maxFiles: -1, maxFileStorageMB: -1 };
        }
    };

    const addFiles = (newFiles: File[]) => {
        if (manageSharedFiles) {
            // Admin can upload without limits
            const uploadedFiles: UploadedFile[] = newFiles.map((file) => ({
                file,
                status: 'pending',
            }));
            setFiles((prev) => [...prev, ...uploadedFiles]);
            return;
        }

        // Check limits for regular users
        const tier = subscription?.tier || 'Free';
        const limits = getLimits(tier);

        // Check file count limit
        const newFileCount = (usage?.files?.totalCount || 0) + newFiles.length;

        if (limits.maxFiles >= 0 && newFileCount > limits.maxFiles) {
            setLimitError(
                `You've reached your file limit (${limits.maxFiles} files). Upgrade to upload more files.`,
            );
            return;
        }

        // Check file size limit
        const totalNewSize = newFiles.reduce((sum, file) => sum + file.size, 0);
        const currentStorageMB = usage?.fileStorage?.megabytes || 0;
        const newStorageMB = currentStorageMB + totalNewSize / (1024 * 1024);

        if (
            limits.maxFileStorageMB >= 0 &&
            newStorageMB > limits.maxFileStorageMB
        ) {
            setLimitError(
                `You've reached your storage limit (${limits.maxFileStorageMB} MB). Upgrade to upload more files.`,
            );
            return;
        }

        setLimitError(null);
        const uploadedFiles: UploadedFile[] = newFiles.map((file) => ({
            file,
            status: 'pending',
        }));
        setFiles((prev) => [...prev, ...uploadedFiles]);
    };

    const removeFile = (index: number) => {
        setFiles((prev) => prev.filter((_, i) => i !== index));
    };

    const handleUpload = async (fileIndices?: number[]) => {
        // If fileIndices is provided, only upload those specific files
        // Otherwise, upload all pending files
        const filesToProcess = fileIndices
            ? fileIndices.map((i) => files[i]).filter((f) => f)
            : files.filter(
                  (f) => f.status === 'pending' || f.status === 'error',
              );

        if (filesToProcess.length === 0) return;

        setIsUploading(true);
        setLimitError(null);

        try {
            // Update files to uploading status
            setFiles((prev) =>
                prev.map((f, i) => {
                    const shouldUpload = fileIndices
                        ? fileIndices.includes(i)
                        : f.status === 'pending' || f.status === 'error';
                    return shouldUpload
                        ? {
                              ...f,
                              status: 'uploading' as const,
                              message: undefined,
                          }
                        : f;
                }),
            );

            const filesToUpload = filesToProcess.map((f) => f.file);
            const formData = new FormData();
            filesToUpload.forEach((file) => {
                formData.append('file', file, file.name);
            });

            let response: Response;
            try {
                response = await fetchWithAuth(
                    manageSharedFiles
                        ? '/api/admin/uploadfiles'
                        : '/api/user/uploadfiles',
                    {
                        method: 'POST',
                        body: formData,
                    },
                );
            } catch (error) {
                // Network error - fetch failed
                const errorMessage =
                    error instanceof Error
                        ? error.message
                        : 'Network error occurred';
                setFiles((prev) =>
                    prev.map((f, i) => {
                        const shouldUpdate = fileIndices
                            ? fileIndices.includes(i)
                            : f.status === 'uploading';
                        return shouldUpdate
                            ? {
                                  ...f,
                                  status: 'error' as const,
                                  message: errorMessage,
                                  errorType: 'network' as const,
                              }
                            : f;
                    }),
                );
                setIsUploading(false);
                return;
            }

            const responseData = await response.json();

            // Check if the entire request failed (e.g., feature gate check failed)
            if (!response.ok || responseData.status === 'error') {
                const errorMessage =
                    responseData.error ||
                    responseData.message ||
                    'Upload failed';
                const errorType = responseData.errorType || 'http';

                // Check if it's a limit error
                if (
                    errorType === 'limit' ||
                    errorMessage.toLowerCase().includes('limit') ||
                    errorMessage.toLowerCase().includes('exceeded')
                ) {
                    setLimitError(errorMessage);
                }

                // Mark all files being uploaded as error
                setFiles((prev) =>
                    prev.map((f, i) => {
                        const shouldUpdate = fileIndices
                            ? fileIndices.includes(i)
                            : f.status === 'uploading';
                        return shouldUpdate
                            ? {
                                  ...f,
                                  status: 'error' as const,
                                  message: errorMessage,
                                  errorType:
                                      (errorType as
                                          | 'network'
                                          | 'limit'
                                          | 'other') || 'other',
                              }
                            : f;
                    }),
                );
                setIsUploading(false);
                return;
            }

            // Process per-file results from backend
            // Backend returns an array of results: { status: "uploaded" | "error", filename, message?, ... }
            if (Array.isArray(responseData)) {
                setFiles((prev) =>
                    prev.map((f, index) => {
                        // Only process files that were being uploaded
                        const wasUploading =
                            fileIndices?.includes(index) ||
                            f.status === 'uploading';

                        if (!wasUploading) {
                            return f;
                        }

                        // Find matching result by filename
                        const result = responseData.find(
                            (r: { filename: string }) =>
                                r.filename === f.file.name,
                        );

                        if (!result) {
                            // No result found - might be a different file or error
                            return {
                                ...f,
                                status: 'error' as const,
                                message: 'No result from server',
                                errorType: 'other' as const,
                            };
                        }

                        if (result.status === 'uploaded') {
                            return {
                                ...f,
                                status: 'success' as const,
                                message: undefined,
                                errorType: undefined,
                            };
                        } else if (result.status === 'error') {
                            const errorMessage =
                                result.message || 'Upload failed';
                            const isLimitError =
                                errorMessage.toLowerCase().includes('limit') ||
                                errorMessage.toLowerCase().includes('exceeded');

                            if (isLimitError) {
                                setLimitError(errorMessage);
                            }

                            return {
                                ...f,
                                status: 'error' as const,
                                message: errorMessage,
                                errorType: isLimitError
                                    ? ('limit' as const)
                                    : ('other' as const),
                            };
                        }

                        return f;
                    }),
                );

                // Check if any files succeeded
                const hasSuccess = responseData.some(
                    (r: { status: string }) => r.status === 'uploaded',
                );
                if (hasSuccess) {
                    onUploadComplete();
                    // Reload usage data after successful upload
                    if (!manageSharedFiles) {
                        await loadSubscriptionData();
                    }
                }
            } else {
                // Unexpected response format
                setFiles((prev) =>
                    prev.map((f, i) => {
                        const shouldUpdate = fileIndices
                            ? fileIndices.includes(i)
                            : f.status === 'uploading';
                        return shouldUpdate
                            ? {
                                  ...f,
                                  status: 'error' as const,
                                  message: 'Unexpected response format',
                                  errorType: 'other' as const,
                              }
                            : f;
                    }),
                );
            }
        } catch (error) {
            // Unexpected error
            const errorMessage =
                error instanceof Error ? error.message : 'Upload failed';
            setFiles((prev) =>
                prev.map((f, i) => {
                    const shouldUpdate = fileIndices
                        ? fileIndices.includes(i)
                        : f.status === 'uploading';
                    return shouldUpdate
                        ? {
                              ...f,
                              status: 'error' as const,
                              message: errorMessage,
                              errorType: 'other' as const,
                          }
                        : f;
                }),
            );
        } finally {
            setIsUploading(false);
        }
    };

    const retryFile = async (index: number) => {
        await handleUpload([index]);
    };

    const clearCompleted = () => {
        setFiles((prev) => prev.filter((f) => f.status !== 'success'));
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

    const tier = subscription?.tier || 'Free';
    const limits = getLimits(tier);
    const currentStorageMB = usage?.fileStorage?.megabytes || 0;

    return (
        <div className="space-y-6">
            {/* Limit Error */}
            {limitError && (
                <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
                    <p className="text-sm text-yellow-800 mb-2">{limitError}</p>
                    {!manageSharedFiles && (
                        <Link
                            href="/app/user/subscription"
                            className="text-sm text-yellow-900 underline font-medium"
                        >
                            Upgrade now →
                        </Link>
                    )}
                </div>
            )}

            {/* Usage Display */}
            {!manageSharedFiles && usage && (
                <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
                    <div className="flex items-center justify-between text-sm">
                        <span className="text-gray-600">File Storage</span>
                        <span className="text-gray-900 font-medium">
                            {Math.round(currentStorageMB)} MB
                            {limits.maxFileStorageMB >= 0 &&
                                ` / ${limits.maxFileStorageMB} MB`}
                            {limits.maxFileStorageMB < 0 && ' (unlimited)'}
                        </span>
                    </div>
                </div>
            )}

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
                        htmlFor="file-upload"
                        className="cursor-pointer text-indigo-600 hover:text-indigo-500 font-medium"
                    >
                        Choose files
                    </label>
                    <input
                        id="file-upload"
                        type="file"
                        multiple
                        onChange={handleFileSelect}
                        className="sr-only"
                    />
                    <span className="text-gray-600"> or drag and drop</span>
                </div>
                <p className="mt-2 text-xs text-gray-500">
                    PDF, DOC, DOCX, TXT up to 10MB each, total of 100MB
                </p>
            </div>

            {/* File List */}
            {files.length > 0 && (
                <div className="space-y-4">
                    <div className="flex items-center justify-between">
                        <h3 className="text-sm font-medium text-gray-900">
                            Files ({files.length})
                        </h3>
                        <button
                            onClick={clearCompleted}
                            className="text-sm text-gray-600 hover:text-gray-900"
                        >
                            Clear completed
                        </button>
                    </div>

                    <ul className="divide-y divide-gray-200 border border-gray-200 rounded-lg">
                        {files.map((uploadedFile, index) => (
                            <li key={index} className="p-4">
                                <div className="flex items-center justify-between">
                                    <div className="flex-1 min-w-0">
                                        <p className="text-sm font-medium text-gray-900 truncate">
                                            {uploadedFile.file.name}
                                        </p>
                                        <p className="text-xs text-gray-500">
                                            {formatFileSize(
                                                uploadedFile.file.size,
                                            )}
                                            {uploadedFile.chunks &&
                                                ` • ${uploadedFile.chunks} chunks`}
                                        </p>
                                        {uploadedFile.message && (
                                            <p
                                                className={`text-xs mt-1 ${
                                                    uploadedFile.status ===
                                                    'error'
                                                        ? 'text-red-600'
                                                        : 'text-gray-500'
                                                }`}
                                            >
                                                {uploadedFile.message}
                                            </p>
                                        )}
                                    </div>
                                    <div className="ml-4 flex items-center space-x-2">
                                        {uploadedFile.status === 'success' && (
                                            <CheckCircleIcon className="h-5 w-5 text-green-500" />
                                        )}
                                        {uploadedFile.status === 'error' && (
                                            <>
                                                <XMarkIcon className="h-5 w-5 text-red-500" />
                                                <button
                                                    onClick={() =>
                                                        retryFile(index)
                                                    }
                                                    disabled={isUploading}
                                                    className="ml-2 text-sm text-indigo-600 hover:text-indigo-700 disabled:text-gray-400 disabled:cursor-not-allowed flex items-center space-x-1"
                                                    title="Retry upload"
                                                >
                                                    <ArrowPathIcon className="h-4 w-4" />
                                                    <span>Retry</span>
                                                </button>
                                            </>
                                        )}
                                        {uploadedFile.status ===
                                            'uploading' && (
                                            <div className="animate-spin h-5 w-5 border-2 border-indigo-500 border-t-transparent rounded-full" />
                                        )}
                                        {uploadedFile.status === 'pending' && (
                                            <button
                                                onClick={() =>
                                                    removeFile(index)
                                                }
                                                className="text-gray-400 hover:text-gray-600"
                                                title="Remove file"
                                            >
                                                <XMarkIcon className="h-5 w-5" />
                                            </button>
                                        )}
                                    </div>
                                </div>
                            </li>
                        ))}
                    </ul>

                    {/* Upload Button */}
                    <div className="flex justify-end">
                        <button
                            onClick={() => handleUpload()}
                            disabled={
                                isUploading ||
                                files.every(
                                    (f) =>
                                        f.status !== 'pending' &&
                                        f.status !== 'error',
                                )
                            }
                            className="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
                        >
                            {isUploading ? 'Uploading...' : 'Upload Files'}
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}
