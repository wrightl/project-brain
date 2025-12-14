'use client';

import Link from 'next/link';
import {
    CloudArrowUpIcon,
    MusicalNoteIcon,
    DocumentArrowUpIcon,
    ClockIcon,
    BookOpenIcon,
} from '@heroicons/react/24/outline';
import { Resource } from '@/_lib/types';
import { VoiceNote } from '@/_lib/types';
import {
    useResources,
    useResourceStatistics,
} from '@/_hooks/queries/use-resources';
import {
    useVoiceNotes,
    useVoiceNoteStatistics,
} from '@/_hooks/queries/use-voicenotes';
import {
    useRecentJournalEntries,
    useJournalEntryCount,
} from '@/_hooks/queries/use-journals';
import { SkeletonCard, SkeletonList } from '@/_components/ui/skeleton';
import { ErrorRetry } from '@/_components/error-retry';

export default function ResourcesPageContent() {
    const {
        data: resourcesResponse,
        isLoading: resourcesLoading,
        error: resourcesError,
    } = useResources({ pageSize: 3 });
    const resources = resourcesResponse?.items || [];
    const {
        data: voiceNotesResponse,
        isLoading: voiceNotesLoading,
        error: voiceNotesError,
    } = useVoiceNotes({ pageSize: 3 });
    const voiceNotes = voiceNotesResponse?.items || [];
    const {
        data: filesCount,
        isLoading: filesCountLoading,
        error: filesCountError,
    } = useResourceStatistics();
    const {
        data: voiceNotesCount,
        isLoading: voiceNotesCountLoading,
        error: voiceNotesCountError,
    } = useVoiceNoteStatistics();
    const {
        data: journalEntries,
        isLoading: journalEntriesLoading,
        error: journalEntriesError,
    } = useRecentJournalEntries(3);
    const {
        data: journalCount,
        isLoading: journalCountLoading,
        error: journalCountError,
    } = useJournalEntryCount();

    const loading =
        resourcesLoading ||
        voiceNotesLoading ||
        filesCountLoading ||
        voiceNotesCountLoading ||
        journalEntriesLoading ||
        journalCountLoading;
    const error =
        resourcesError ||
        voiceNotesError ||
        filesCountError ||
        voiceNotesCountError ||
        journalEntriesError ||
        journalCountError;

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-GB', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
        });
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

    const stats = [
        {
            name: 'Total Files Uploaded',
            value: (filesCount?.count ?? 0).toString(),
            icon: DocumentArrowUpIcon,
        },
        {
            name: 'Total Voice Notes',
            value: (voiceNotesCount?.count ?? 0).toString(),
            icon: MusicalNoteIcon,
        },
        {
            name: 'Total Journal Entries',
            value: (journalCount?.count ?? 0).toString(),
            icon: BookOpenIcon,
        },
    ];

    const quickActions = [
        {
            title: 'Manage Files',
            description: 'Upload, view, and manage your documents',
            href: '/app/user/manage-files',
            icon: CloudArrowUpIcon,
            color: 'bg-indigo-500',
        },
        {
            title: 'Manage Voice Notes',
            description: 'Record, upload, and manage your voice notes',
            href: '/app/user/voicenotes',
            icon: MusicalNoteIcon,
            color: 'bg-purple-500',
        },
        {
            title: 'Journal Entries',
            description: 'View your journal entries',
            href: '/app/user/journal',
            icon: BookOpenIcon,
            color: 'bg-green-500',
        },
        {
            title: 'Add Journal Entry',
            description: 'Create a new journal entry',
            href: '/app/user/journal/new',
            icon: BookOpenIcon,
            color: 'bg-green-500',
        },
    ];

    // Resources and voice notes are already limited to 3 from the API
    const recentResources = resources;
    const recentVoiceNotes = voiceNotes;

    if (loading) {
        return (
            <div className="space-y-8">
                <div>
                    <h1 className="text-3xl font-bold text-gray-900">
                        My Resources
                    </h1>
                    <p className="mt-2 text-sm text-gray-600">
                        Manage your files and voice notes
                    </p>
                </div>
                <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                    <SkeletonCard />
                    <SkeletonCard />
                </div>
                <SkeletonList count={3} />
            </div>
        );
    }

    if (error) {
        return (
            <div className="space-y-8">
                <div>
                    <h1 className="text-3xl font-bold text-gray-900">
                        My Resources
                    </h1>
                    <p className="mt-2 text-sm text-gray-600">
                        Manage your files and voice notes
                    </p>
                </div>
                <ErrorRetry
                    error={error}
                    onRetry={() => {
                        // React Query will automatically retry on refetch
                        window.location.reload();
                    }}
                />
            </div>
        );
    }

    return (
        <div className="space-y-8">
            <div>
                <h1 className="text-3xl font-bold text-gray-900">
                    My Resources
                </h1>
                <p className="mt-2 text-sm text-gray-600">
                    Manage your files and voice notes
                </p>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-3">
                {stats.map((stat) => {
                    const Icon = stat.icon;
                    return (
                        <div
                            key={stat.name}
                            className="bg-white overflow-hidden shadow rounded-lg"
                        >
                            <div className="p-5">
                                <div className="flex items-center">
                                    <div className="flex-shrink-0">
                                        <Icon
                                            className="h-6 w-6 text-gray-400"
                                            aria-hidden="true"
                                        />
                                    </div>
                                    <div className="ml-5 w-0 flex-1">
                                        <dl>
                                            <dt className="text-sm font-medium text-gray-500 truncate">
                                                {stat.name}
                                            </dt>
                                            <dd className="text-lg font-semibold text-gray-900">
                                                {stat.value}
                                            </dd>
                                        </dl>
                                    </div>
                                </div>
                            </div>
                        </div>
                    );
                })}
            </div>

            {/* Quick Actions */}
            <div>
                <h2 className="text-xl font-semibold text-gray-900 mb-4">
                    Quick Actions
                </h2>
                <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-2">
                    {quickActions.map((action) => {
                        const Icon = action.icon;
                        return (
                            <Link
                                key={action.href}
                                href={action.href || ''}
                                className="relative group bg-white p-6 rounded-lg shadow hover:shadow-lg transition-shadow"
                            >
                                <div>
                                    <span
                                        className={`${action.color} rounded-lg inline-flex p-3 ring-4 ring-white`}
                                    >
                                        <Icon
                                            className="h-6 w-6 text-white"
                                            aria-hidden="true"
                                        />
                                    </span>
                                </div>
                                <div className="mt-4">
                                    <h3 className="text-lg font-medium text-gray-900 group-hover:text-indigo-600">
                                        {action.title}
                                    </h3>
                                    <p className="mt-2 text-sm text-gray-500">
                                        {action.description}
                                    </p>
                                </div>
                            </Link>
                        );
                    })}
                </div>
            </div>

            {/* Recent Files */}
            <div>
                <div className="flex items-center justify-between mb-4">
                    <h2 className="text-xl font-semibold text-gray-900">
                        Recent Files
                    </h2>
                    {resources.length > 0 && (
                        <Link
                            href="/app/user/manage-files"
                            className="text-sm font-medium text-indigo-600 hover:text-indigo-500"
                        >
                            View all
                        </Link>
                    )}
                </div>
                {recentResources.length === 0 ? (
                    <div className="bg-white shadow rounded-lg p-8 text-center">
                        <DocumentArrowUpIcon className="mx-auto h-12 w-12 text-gray-400" />
                        <h3 className="mt-2 text-sm font-medium text-gray-900">
                            No files uploaded yet
                        </h3>
                        <p className="mt-1 text-sm text-gray-500">
                            Start by uploading your first file.
                        </p>
                        <div className="mt-6">
                            <Link
                                href="/app/user/manage-files"
                                className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
                            >
                                Upload Files
                            </Link>
                        </div>
                    </div>
                ) : (
                    <div className="bg-white shadow rounded-lg overflow-hidden">
                        <ul className="divide-y divide-gray-200">
                            {recentResources.map((resource: Resource) => (
                                <li key={resource.id} className="p-4">
                                    <div className="flex items-center justify-between">
                                        <div className="flex-1 min-w-0">
                                            <div className="flex items-center">
                                                <DocumentArrowUpIcon className="h-5 w-5 text-gray-400 mr-3 flex-shrink-0" />
                                                <div className="flex-1 min-w-0">
                                                    <p className="text-sm font-medium text-gray-900 truncate">
                                                        {resource.fileName}
                                                    </p>
                                                    <div className="mt-1 flex items-center text-sm text-gray-500">
                                                        <ClockIcon className="h-4 w-4 mr-1" />
                                                        {formatDate(
                                                            resource.createdAt
                                                        )}{' '}
                                                        •{' '}
                                                        {formatFileSize(
                                                            resource.sizeInBytes
                                                        )}
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </li>
                            ))}
                        </ul>
                    </div>
                )}
            </div>

            {/* Recent Voice Notes */}
            <div>
                <div className="flex items-center justify-between mb-4">
                    <h2 className="text-xl font-semibold text-gray-900">
                        Recent Voice Notes
                    </h2>
                    {voiceNotes.length > 0 && (
                        <Link
                            href="/app/user/voicenotes"
                            className="text-sm font-medium text-indigo-600 hover:text-indigo-500"
                        >
                            View all
                        </Link>
                    )}
                </div>
                {recentVoiceNotes.length === 0 ? (
                    <div className="bg-white shadow rounded-lg p-8 text-center">
                        <MusicalNoteIcon className="mx-auto h-12 w-12 text-gray-400" />
                        <h3 className="mt-2 text-sm font-medium text-gray-900">
                            No voice notes yet
                        </h3>
                        <p className="mt-1 text-sm text-gray-500">
                            Start by recording or uploading your first voice
                            note.
                        </p>
                        <div className="mt-6">
                            <Link
                                href="/app/user/voicenotes"
                                className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
                            >
                                Manage Voice Notes
                            </Link>
                        </div>
                    </div>
                ) : (
                    <div className="bg-white shadow rounded-lg overflow-hidden">
                        <ul className="divide-y divide-gray-200">
                            {recentVoiceNotes.map((voiceNote: VoiceNote) => (
                                <li key={voiceNote.id} className="p-4">
                                    <div className="flex items-center justify-between">
                                        <div className="flex-1 min-w-0">
                                            <div className="flex items-center">
                                                <MusicalNoteIcon className="h-5 w-5 text-gray-400 mr-3 flex-shrink-0" />
                                                <div className="flex-1 min-w-0">
                                                    <p className="text-sm font-medium text-gray-900 truncate">
                                                        {voiceNote.fileName}
                                                    </p>
                                                    <div className="mt-1 flex items-center text-sm text-gray-500">
                                                        <ClockIcon className="h-4 w-4 mr-1" />
                                                        {formatDate(
                                                            voiceNote.createdAt
                                                        )}
                                                        {voiceNote.duration && (
                                                            <>
                                                                {' '}
                                                                •{' '}
                                                                {Math.round(
                                                                    voiceNote.duration
                                                                )}
                                                                s
                                                            </>
                                                        )}
                                                        {voiceNote.fileSize && (
                                                            <>
                                                                {' '}
                                                                •{' '}
                                                                {formatFileSize(
                                                                    voiceNote.fileSize
                                                                )}
                                                            </>
                                                        )}
                                                    </div>
                                                    {voiceNote.description && (
                                                        <p className="mt-1 text-sm text-gray-600 truncate">
                                                            {
                                                                voiceNote.description
                                                            }
                                                        </p>
                                                    )}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </li>
                            ))}
                        </ul>
                    </div>
                )}
            </div>

            {/* Recent Journal Entries */}
            <div>
                <div className="flex items-center justify-between mb-4">
                    <h2 className="text-xl font-semibold text-gray-900">
                        Recent Journal Entries
                    </h2>
                    {journalEntries && journalEntries.length > 0 && (
                        <Link
                            href="/app/user/journal"
                            className="text-sm font-medium text-indigo-600 hover:text-indigo-500"
                        >
                            View all
                        </Link>
                    )}
                </div>
                {!journalEntries || journalEntries.length === 0 ? (
                    <div className="bg-white shadow rounded-lg p-8 text-center">
                        <BookOpenIcon className="mx-auto h-12 w-12 text-gray-400" />
                        <h3 className="mt-2 text-sm font-medium text-gray-900">
                            No journal entries yet
                        </h3>
                        <p className="mt-1 text-sm text-gray-500">
                            Start by creating your first journal entry.
                        </p>
                        <div className="mt-6">
                            <Link
                                href="/app/user/journal/new"
                                className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
                            >
                                Create Journal Entry
                            </Link>
                        </div>
                    </div>
                ) : (
                    <div className="bg-white shadow rounded-lg overflow-hidden">
                        <ul className="divide-y divide-gray-200">
                            {journalEntries.map((entry) => (
                                <li key={entry.id} className="p-4">
                                    <Link
                                        href={`/app/user/journal/${entry.id}`}
                                        className="block"
                                    >
                                        <div className="flex items-center justify-between">
                                            <div className="flex-1 min-w-0">
                                                <div className="flex items-center">
                                                    <BookOpenIcon className="h-5 w-5 text-gray-400 mr-3 flex-shrink-0" />
                                                    <div className="flex-1 min-w-0">
                                                        <p className="text-sm font-medium text-gray-900 truncate">
                                                            {entry.summary ||
                                                                'Untitled Entry'}
                                                        </p>
                                                        <div className="mt-1 flex items-center text-sm text-gray-500">
                                                            <ClockIcon className="h-4 w-4 mr-1" />
                                                            {formatDate(
                                                                entry.createdAt
                                                            )}
                                                        </div>
                                                        {entry.tags &&
                                                            entry.tags.length >
                                                                0 && (
                                                                <div className="mt-1 flex flex-wrap gap-1">
                                                                    {entry.tags
                                                                        .slice(
                                                                            0,
                                                                            3
                                                                        )
                                                                        .map(
                                                                            (
                                                                                tag
                                                                            ) => (
                                                                                <span
                                                                                    key={
                                                                                        tag.id
                                                                                    }
                                                                                    className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-indigo-100 text-indigo-800"
                                                                                >
                                                                                    {
                                                                                        tag.name
                                                                                    }
                                                                                </span>
                                                                            )
                                                                        )}
                                                                    {entry.tags
                                                                        .length >
                                                                        3 && (
                                                                        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-600">
                                                                            +
                                                                            {entry
                                                                                .tags
                                                                                .length -
                                                                                3}{' '}
                                                                            more
                                                                        </span>
                                                                    )}
                                                                </div>
                                                            )}
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </Link>
                                </li>
                            ))}
                        </ul>
                    </div>
                )}
            </div>
        </div>
    );
}
