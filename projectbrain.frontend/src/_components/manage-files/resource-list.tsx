'use client';

import { useState, useEffect, useCallback } from 'react';
import { ReindexResult, Resource } from '@/_lib/types';
import { TrashIcon } from '@heroicons/react/24/outline';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';

export default function ResourceList({
    triggerRefresh,
    manageSharedFiles,
}: {
    manageSharedFiles: boolean;
    triggerRefresh: boolean;
}) {
    const [resources, setResources] = useState<Resource[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [deletingId, setDeletingId] = useState<string | null>(null);

    const fetchResources = useCallback(async () => {
        try {
            setLoading(true);
            setError(null);
            const response = await fetchWithAuth(
                manageSharedFiles
                    ? '/api/admin/resources'
                    : '/api/user/resources'
            );
            if (!response.ok) {
                throw new Error('Failed to load resources');
            }
            const data = await response.json();
            setResources(data);
        } catch (err) {
            setError(
                err instanceof Error ? err.message : 'Failed to load resources'
            );
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        fetchResources();
    }, [triggerRefresh]);

    useEffect(() => {
        fetchResources();
    }, [fetchResources]);

    // useEffect(() => {
    //     // Listen for custom event from file upload form
    //     const handleResourcesUpdated = () => {
    //         fetchResources();
    //     };

    //     window.addEventListener('resourcesUpdated', handleResourcesUpdated);

    //     return () => {
    //         window.removeEventListener(
    //             'resourcesUpdated',
    //             handleResourcesUpdated
    //         );
    //     };
    // }, [fetchResources]);

    const handleDelete = async (resourceId: string) => {
        if (
            !confirm(
                'Are you sure you want to delete this file? This action cannot be undone.'
            )
        ) {
            return;
        }

        try {
            setDeletingId(resourceId);
            const response = await fetchWithAuth(
                manageSharedFiles
                    ? `/api/admin/resources/${resourceId}`
                    : `/api/user/resources/${resourceId}`,
                {
                    method: 'DELETE',
                }
            );
            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.error || 'Failed to delete resource');
            }
            // Remove the deleted resource from the list
            setResources((prev) =>
                prev.filter((resource) => resource.id !== resourceId)
            );
        } catch (err) {
            alert(
                err instanceof Error ? err.message : 'Failed to delete resource'
            );
        } finally {
            setDeletingId(null);
        }
    };

    const handleReindex = async () => {
        try {
            setLoading(true);
            const response = await fetchWithAuth(
                manageSharedFiles
                    ? '/api/admin/resources/reindex'
                    : '/api/user/resources/reindex',
                {
                    method: 'POST',
                }
            );

            if (!response.ok) {
                throw new Error('Failed to reindex resources');
            }

            const result: ReindexResult = await response.json();
            if (result.status === 'success') {
                alert(
                    `Files reindexed successfully! ${result.filesReindexed} files processed.`
                );
                fetchResources();
            } else {
                alert('Failed to reindex files');
            }
        } catch (error) {
            alert(
                `Error: ${
                    error instanceof Error
                        ? error.message
                        : 'Failed to reindex files'
                }`
            );
        } finally {
            setLoading(false);
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

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-GB', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        });
    };

    if (loading) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <h2 className="text-lg font-medium text-gray-900 mb-4">
                    Uploaded Files
                </h2>
                <div className="text-center py-8">
                    <div className="animate-spin h-8 w-8 border-2 border-indigo-500 border-t-transparent rounded-full mx-auto" />
                    <p className="mt-2 text-sm text-gray-500">
                        Loading files...
                    </p>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <h2 className="text-lg font-medium text-gray-900 mb-4">
                    Uploaded Files
                </h2>
                <div className="text-center py-8">
                    <p className="text-sm text-red-600">{error}</p>
                    <button
                        onClick={fetchResources}
                        className="mt-4 px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 transition-colors"
                    >
                        Retry
                    </button>
                </div>
            </div>
        );
    }

    return (
        <div className="bg-white shadow rounded-lg p-6">
            <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-medium text-gray-900">
                    Uploaded Files
                </h2>
                <div className="flex items-center justify-between mb-4 gap-4">
                    <button
                        onClick={handleReindex}
                        className="text-sm text-indigo-600 hover:text-indigo-700 font-medium"
                    >
                        Reindex
                    </button>
                    <button
                        onClick={fetchResources}
                        className="text-sm text-indigo-600 hover:text-indigo-700 font-medium"
                    >
                        Refresh
                    </button>
                </div>
            </div>

            {resources.length === 0 ? (
                <div className="text-center py-8">
                    <p className="text-sm text-gray-500">
                        No files uploaded yet. Upload your first file above.
                    </p>
                </div>
            ) : (
                <div className="overflow-x-auto">
                    <table className="min-w-full divide-y divide-gray-200">
                        <thead className="bg-gray-50">
                            <tr>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    File Name
                                </th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Size
                                </th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Uploaded
                                </th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    Actions
                                </th>
                            </tr>
                        </thead>
                        <tbody className="bg-white divide-y divide-gray-200">
                            {resources.map((resource) => (
                                <tr
                                    key={resource.id}
                                    className="hover:bg-gray-50"
                                >
                                    <td className="px-6 py-4 whitespace-nowrap">
                                        <div className="text-sm font-medium text-gray-900">
                                            {resource.fileName}
                                        </div>
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap">
                                        <div className="text-sm text-gray-500">
                                            {formatFileSize(
                                                resource.sizeInBytes
                                            )}
                                        </div>
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap">
                                        <div className="text-sm text-gray-500">
                                            {formatDate(resource.createdAt)}
                                        </div>
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                                        <button
                                            onClick={() =>
                                                handleDelete(resource.id)
                                            }
                                            disabled={
                                                deletingId === resource.id
                                            }
                                            className="inline-flex items-center text-red-600 hover:text-red-900 disabled:text-gray-400 disabled:cursor-not-allowed transition-colors"
                                        >
                                            {deletingId === resource.id ? (
                                                <>
                                                    <div className="animate-spin h-4 w-4 border-2 border-red-600 border-t-transparent rounded-full mr-2" />
                                                    Deleting...
                                                </>
                                            ) : (
                                                <>
                                                    <TrashIcon className="h-4 w-4 mr-1" />
                                                    Delete
                                                </>
                                            )}
                                        </button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </div>
    );
}
