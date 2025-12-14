'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import {
    useJournalEntries,
    useDeleteJournalEntry,
} from '@/_hooks/queries/use-journals';
import { JournalEntry } from '@/_services/journal-service';
import {
    PencilIcon,
    TrashIcon,
    PlusIcon,
    ClockIcon,
} from '@heroicons/react/24/outline';
import toast from 'react-hot-toast';

export default function JournalList() {
    const router = useRouter();
    const [page, setPage] = useState(1);
    const pageSize = 20;

    const {
        data: journalResponse,
        isLoading,
        error,
    } = useJournalEntries({ page, pageSize });

    const deleteMutation = useDeleteJournalEntry();

    const journalEntries = journalResponse?.items || [];
    const totalPages = journalResponse?.totalPages || 0;

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-GB', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
        });
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Are you sure you want to delete this journal entry?')) {
            return;
        }

        try {
            await deleteMutation.mutateAsync(id);
            toast.success('Journal entry deleted successfully');
        } catch (error) {
            toast.error(
                error instanceof Error
                    ? error.message
                    : 'Failed to delete journal entry'
            );
        }
    };

    if (isLoading) {
        return (
            <div className="flex justify-center items-center py-12">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600" />
            </div>
        );
    }

    if (error) {
        return (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                <p className="text-sm text-red-800">
                    {error instanceof Error
                        ? error.message
                        : 'Failed to load journal entries'}
                </p>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <h2 className="text-lg font-semibold text-gray-900">
                    Your Journal Entries
                </h2>
                <Link
                    href="/app/user/journal/new"
                    className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
                >
                    <PlusIcon className="h-5 w-5 mr-2" />
                    New Entry
                </Link>
            </div>

            {journalEntries.length === 0 ? (
                <div className="text-center py-12 bg-white rounded-lg shadow">
                    <p className="text-gray-500 text-lg">No journal entries yet</p>
                    <p className="text-gray-400 text-sm mt-2">
                        Start by creating your first journal entry
                    </p>
                    <Link
                        href="/app/user/journal/new"
                        className="mt-4 inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
                    >
                        <PlusIcon className="h-5 w-5 mr-2" />
                        Create First Entry
                    </Link>
                </div>
            ) : (
                <>
                    <div className="bg-white shadow rounded-lg overflow-hidden">
                        <ul className="divide-y divide-gray-200">
                            {journalEntries.map((entry: JournalEntry) => (
                                <li key={entry.id} className="p-6 hover:bg-gray-50">
                                    <div className="flex items-start justify-between">
                                        <div className="flex-1 min-w-0">
                                            <Link
                                                href={`/app/user/journal/${entry.id}`}
                                                className="block"
                                            >
                                                <div className="flex items-center">
                                                    <div className="flex-1 min-w-0">
                                                        <p className="text-sm font-medium text-gray-900 truncate">
                                                            {entry.summary ||
                                                                'Untitled Entry'}
                                                        </p>
                                                        <div className="mt-2 flex items-center text-sm text-gray-500">
                                                            <ClockIcon className="h-4 w-4 mr-1" />
                                                            {formatDate(entry.createdAt)}
                                                        </div>
                                                        {entry.tags &&
                                                            entry.tags.length > 0 && (
                                                                <div className="mt-2 flex flex-wrap gap-2">
                                                                    {entry.tags.map(
                                                                        (tag) => (
                                                                            <span
                                                                                key={
                                                                                    tag.id
                                                                                }
                                                                                className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-indigo-100 text-indigo-800"
                                                                            >
                                                                                {
                                                                                    tag.name
                                                                                }
                                                                            </span>
                                                                        )
                                                                    )}
                                                                </div>
                                                            )}
                                                        {entry.content && (
                                                            <p className="mt-2 text-sm text-gray-600 line-clamp-2">
                                                                {entry.content.substring(
                                                                    0,
                                                                    200
                                                                )}
                                                                {entry.content.length >
                                                                200 && '...'}
                                                            </p>
                                                        )}
                                                    </div>
                                                </div>
                                            </Link>
                                        </div>
                                        <div className="ml-4 flex items-center space-x-2">
                                            <Link
                                                href={`/app/user/journal/${entry.id}`}
                                                className="text-gray-400 hover:text-indigo-600"
                                                title="Edit"
                                            >
                                                <PencilIcon className="h-5 w-5" />
                                            </Link>
                                            <button
                                                onClick={(e) => {
                                                    e.preventDefault();
                                                    handleDelete(entry.id);
                                                }}
                                                className="text-gray-400 hover:text-red-600"
                                                title="Delete"
                                                disabled={
                                                    deleteMutation.isPending
                                                }
                                            >
                                                <TrashIcon className="h-5 w-5" />
                                            </button>
                                        </div>
                                    </div>
                                </li>
                            ))}
                        </ul>
                    </div>

                    {/* Pagination */}
                    {totalPages > 1 && (
                        <div className="flex items-center justify-between border-t border-gray-200 bg-white px-4 py-3 sm:px-6">
                            <div className="flex flex-1 justify-between sm:hidden">
                                <button
                                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                                    disabled={page === 1}
                                    className="relative inline-flex items-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                                >
                                    Previous
                                </button>
                                <button
                                    onClick={() =>
                                        setPage((p) =>
                                            Math.min(totalPages, p + 1)
                                        )
                                    }
                                    disabled={page === totalPages}
                                    className="relative ml-3 inline-flex items-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                                >
                                    Next
                                </button>
                            </div>
                            <div className="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
                                <div>
                                    <p className="text-sm text-gray-700">
                                        Page{' '}
                                        <span className="font-medium">
                                            {page}
                                        </span>{' '}
                                        of{' '}
                                        <span className="font-medium">
                                            {totalPages}
                                        </span>
                                    </p>
                                </div>
                                <div>
                                    <nav
                                        className="isolate inline-flex -space-x-px rounded-md shadow-sm"
                                        aria-label="Pagination"
                                    >
                                        <button
                                            onClick={() =>
                                                setPage((p) => Math.max(1, p - 1))
                                            }
                                            disabled={page === 1}
                                            className="relative inline-flex items-center rounded-l-md px-2 py-2 text-gray-400 ring-1 ring-inset ring-gray-300 hover:bg-gray-50 focus:z-20 focus:outline-offset-0 disabled:opacity-50 disabled:cursor-not-allowed"
                                        >
                                            Previous
                                        </button>
                                        <button
                                            onClick={() =>
                                                setPage((p) =>
                                                    Math.min(totalPages, p + 1)
                                                )
                                            }
                                            disabled={page === totalPages}
                                            className="relative inline-flex items-center rounded-r-md px-2 py-2 text-gray-400 ring-1 ring-inset ring-gray-300 hover:bg-gray-50 focus:z-20 focus:outline-offset-0 disabled:opacity-50 disabled:cursor-not-allowed"
                                        >
                                            Next
                                        </button>
                                    </nav>
                                </div>
                            </div>
                        </div>
                    )}
                </>
            )}
        </div>
    );
}

