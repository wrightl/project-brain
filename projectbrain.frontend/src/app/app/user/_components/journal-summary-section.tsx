'use client';

import Link from 'next/link';
import {
    useJournalEntryCount,
    useRecentJournalEntries,
} from '@/_hooks/queries/use-journals';
import {
    ClockIcon,
    DocumentTextIcon,
    PlusIcon,
} from '@heroicons/react/24/outline';

export default function JournalSummarySection() {
    const { data: countData, isLoading: loadingCount } = useJournalEntryCount();
    const { data: recentEntries, isLoading: loadingRecent } =
        useRecentJournalEntries(3);

    const count = countData?.count ?? 0;
    const entries = recentEntries ?? [];

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-GB', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
        });
    };

    return (
        <section className="relative overflow-hidden rounded-lg bg-white p-6 shadow border border-gray-300">
            <div
                aria-hidden="true"
                className="pointer-events-none absolute inset-0 opacity-[0.06]"
                style={{ background: 'var(--blue-aqua-gradient)' }}
            />

            <div className="relative">
                <div className="flex items-start justify-between gap-6">
                    <div className="min-w-0 flex items-start gap-3">
                        <div className="mt-0.5 flex h-10 w-10 items-center justify-center rounded-md bg-[color:var(--light-aluminium)]">
                            <DocumentTextIcon className="h-5 w-5 text-[color:var(--blue)]" />
                        </div>

                        <div className="min-w-0">
                            <h2 className="text-lg font-semibold text-gray-900">
                                Journals
                            </h2>
                            <p className="mt-1 text-sm text-gray-600">
                                {loadingCount ? (
                                    'Loading…'
                                ) : (
                                    <>
                                        You’ve created{' '}
                                        <span className="font-semibold text-gray-900">
                                            {count}
                                        </span>{' '}
                                        entr{count === 1 ? 'y' : 'ies'}.
                                    </>
                                )}
                            </p>
                        </div>
                    </div>

                    <div className="flex items-center gap-2">
                        <Link
                            href="/app/user/journal/new"
                            className="inline-flex items-center px-3 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
                        >
                            <PlusIcon className="h-5 w-5 mr-2" />
                            New entry
                        </Link>
                        <Link
                            href="/app/user/journal"
                            className="inline-flex items-center px-3 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
                        >
                            View all
                        </Link>
                    </div>
                </div>

                <div className="mt-6">
                    {loadingRecent ? (
                        <p className="mt-2 text-sm text-gray-500">Loading…</p>
                    ) : entries.length === 0 ? (
                        <p className="mt-2 text-sm text-gray-500">
                            No journal entries yet.
                        </p>
                    ) : (
                        <ul className="mt-3 space-y-3">
                            {entries.map((entry) => {
                                const preview =
                                    entry.summary ??
                                    (entry.content ? entry.content.trim() : '');

                                return (
                                    <li
                                        key={entry.id}
                                        className="rounded-md border border-gray-200 p-3 hover:bg-gray-50"
                                    >
                                        <Link
                                            href={`/app/user/journal/${entry.id}`}
                                            className="block"
                                        >
                                            <div className="flex items-center justify-between gap-4">
                                                <p className="text-sm font-medium text-gray-900 line-clamp-1">
                                                    {preview || 'Untitled entry'}
                                                </p>
                                                <div className="flex items-center text-xs text-gray-500 shrink-0">
                                                    <ClockIcon className="h-4 w-4 mr-1" />
                                                    {formatDate(entry.createdAt)}
                                                </div>
                                            </div>
                                            {/* {entry.content && (
                                                <p className="mt-2 text-sm text-gray-600 line-clamp-2">
                                                    {entry.content}
                                                </p>
                                            )} */}
                                        </Link>
                                    </li>
                                );
                            })}
                        </ul>
                    )}
                </div>
            </div>
        </section>
    );
}
