'use client';

import { FireIcon, SparklesIcon, TrophyIcon } from '@heroicons/react/24/solid';
import { FaceSmileIcon, HomeIcon } from '@heroicons/react/24/outline';
import { useStreakSummary } from '@/_hooks/queries/use-goals';
import { useJournalStreakSummary } from '@/_hooks/queries/use-journals';

export default function WelcomeSection({
    displayName,
}: {
    displayName: string;
}) {
    const { data, isLoading } = useStreakSummary();
    const { data: journalStreak, isLoading: journalStreakLoading } =
        useJournalStreakSummary();

    const current = data?.currentStreak ?? 0;
    const longest = Math.max(data?.longestStreak ?? 0, current);
    const pct =
        longest > 0 ? Math.min(100, Math.round((current / longest) * 100)) : 0;

    const journalCurrent = journalStreak?.currentStreak ?? 0;
    const journalLongest = Math.max(journalStreak?.longestStreak ?? 0, journalCurrent);

    return (
        <section className="relative overflow-hidden rounded-lg bg-white p-6 shadow border border-gray-300">
            <div
                aria-hidden="true"
                className="pointer-events-none absolute inset-0 opacity-[0.06]"
                style={{ background: 'var(--indigo-aqua-gradient)' }}
            />

            <div className="relative">
                <div className="flex items-start justify-between gap-6">
                    <div className="min-w-0 flex items-start gap-4">
                        <div className="mt-0.5 flex h-10 w-10 items-center justify-center rounded-md bg-[color:var(--light-aluminium)]">
                            <HomeIcon className="h-5 w-5 text-[color:var(--indigo)]" />
                        </div>

                        <div className="min-w-0">
                            <h1 className="text-2xl sm:text-3xl font-bold text-gray-900">
                                Welcome back, {displayName}
                            </h1>
                            <p className="mt-2 text-sm text-gray-600">
                                You’re doing the work—small steps count. Let’s keep
                                building momentum today.
                            </p>
                        </div>
                    </div>

                    <div className="flex items-center gap-2">
                        {current >= 7 ? (
                            <TrophyIcon className="h-8 w-8 text-[color:var(--indigo)]" />
                        ) : current >= 3 ? (
                            <FireIcon className="h-8 w-8 text-[color:var(--indigo)]" />
                        ) : current >= 1 ? (
                            <SparklesIcon className="h-8 w-8 text-[color:var(--indigo)]" />
                        ) : (
                            <FaceSmileIcon className="h-8 w-8 text-[color:var(--indigo)]" />
                        )}
                    </div>
                </div>

                <div className="mt-6">
                    <div className="flex items-center justify-between">
                        <h2 className="text-sm font-semibold text-gray-900">
                            Daily goal streak
                        </h2>
                        {!isLoading && (
                            <p className="text-sm text-gray-700">
                                <span className="font-semibold text-gray-900">
                                    {current}
                                </span>{' '}
                                day{current === 1 ? '' : 's'}
                                {longest > 0 && (
                                    <>
                                        {' '}
                                        <span className="text-gray-500">
                                            /
                                        </span>{' '}
                                        <span className="font-semibold text-gray-900">
                                            {longest}
                                        </span>{' '}
                                        best
                                    </>
                                )}
                            </p>
                        )}
                    </div>

                    <div className="mt-2 w-full bg-gray-200 rounded-full h-2">
                        <div
                            className="h-2 rounded-full transition-all duration-300 bg-[color:var(--indigo)]"
                            style={{ width: `${isLoading ? 0 : pct}%` }}
                            aria-hidden="true"
                        />
                    </div>

                    <p className="mt-2 text-xs text-gray-500">
                        {isLoading
                            ? 'Loading your streak…'
                            : longest === 0
                              ? 'Complete all your goals today to start your streak.'
                              : `You’re ${Math.max(0, longest - current)} day${
                                    longest - current === 1 ? '' : 's'
                                } away from your best streak.`}
                    </p>
                </div>

                <div className="mt-6">
                    <div className="flex items-center justify-between">
                        <h2 className="text-sm font-semibold text-gray-900">
                            Journal streak
                        </h2>
                        {!journalStreakLoading && (
                            <p className="text-sm text-gray-700">
                                <span className="font-semibold text-gray-900">
                                    {journalCurrent}
                                </span>{' '}
                                day{journalCurrent === 1 ? '' : 's'}
                                {journalLongest > 0 && (
                                    <>
                                        {' '}
                                        <span className="text-gray-500">
                                            /
                                        </span>{' '}
                                        <span className="font-semibold text-gray-900">
                                            {journalLongest}
                                        </span>{' '}
                                        best
                                    </>
                                )}
                            </p>
                        )}
                    </div>

                    <p className="mt-2 text-xs text-gray-500">
                        {journalStreakLoading
                            ? 'Loading your journal streak…'
                            : journalLongest === 0
                              ? 'Write a journal entry today to start your streak.'
                              : `You’re ${Math.max(
                                    0,
                                    journalLongest - journalCurrent
                                )} day${
                                    journalLongest - journalCurrent === 1 ? '' : 's'
                                } away from your best journal streak.`}
                    </p>
                </div>
            </div>
        </section>
    );
}
