'use client';

import { FireIcon, SparklesIcon, TrophyIcon } from '@heroicons/react/24/solid';
import { FaceSmileIcon } from '@heroicons/react/24/outline';
import { useStreakSummary } from '@/_hooks/queries/use-goals';

export default function WelcomeSection({
    displayName,
}: {
    displayName: string;
}) {
    const { data, isLoading } = useStreakSummary();

    const current = data?.currentStreak ?? 0;
    const longest = Math.max(data?.longestStreak ?? 0, current);
    const pct =
        longest > 0 ? Math.min(100, Math.round((current / longest) * 100)) : 0;

    return (
        <section className="bg-white shadow rounded-lg p-6">
            <div className="flex items-start justify-between gap-6">
                <div className="min-w-0">
                    <h1 className="text-2xl sm:text-3xl font-bold text-gray-900">
                        Welcome back, {displayName}
                    </h1>
                    <p className="mt-2 text-sm text-gray-600">
                        You’re doing the work—small steps count. Let’s keep
                        building momentum today.
                    </p>
                </div>

                <div className="flex items-center gap-2 text-gray-900">
                    {current >= 7 ? (
                        <TrophyIcon className="h-8 w-8 text-indigo-600" />
                    ) : current >= 3 ? (
                        <FireIcon className="h-8 w-8 text-indigo-600" />
                    ) : current >= 1 ? (
                        <SparklesIcon className="h-8 w-8 text-indigo-600" />
                    ) : (
                        <FaceSmileIcon className="h-8 w-8 text-indigo-600" />
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
                        className="h-2 rounded-full bg-indigo-600 transition-all duration-300"
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
        </section>
    );
}
