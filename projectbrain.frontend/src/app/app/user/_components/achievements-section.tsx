'use client';

import { TrophyIcon } from '@heroicons/react/24/outline';
import { useAchievements } from '@/_hooks/queries/use-achievements';

export default function AchievementsSection() {
    const { data, isLoading } = useAchievements();

    return (
        <section className="relative overflow-hidden rounded-lg bg-white p-6 shadow border border-gray-300">
            <div
                aria-hidden="true"
                className="pointer-events-none absolute inset-0 opacity-[0.06]"
                style={{ background: 'var(--yellow-mandarine-gradient)' }}
            />

            <div className="relative">
                <div className="flex items-start justify-between gap-4">
                    <div className="flex items-start gap-3">
                        <div className="mt-0.5 flex h-10 w-10 items-center justify-center rounded-md bg-[color:var(--light-aluminium)]">
                            <TrophyIcon className="h-5 w-5 text-[color:var(--yellow)]" />
                        </div>

                        <div>
                            <h2 className="text-xl font-semibold text-gray-900">
                                Achievements
                            </h2>
                            <p className="mt-1 text-sm text-gray-600">
                                Celebrate your progress as you go.
                            </p>
                        </div>
                    </div>
                </div>

                {isLoading ? (
                    <div className="mt-6 space-y-3">
                        <div className="h-14 bg-gray-100 rounded-md animate-pulse" />
                        <div className="h-14 bg-gray-100 rounded-md animate-pulse" />
                    </div>
                ) : (data?.items?.length ?? 0) === 0 ? (
                    <div className="mt-6 rounded-md border border-dashed border-gray-300 p-6">
                        <p className="text-sm text-gray-700">
                            No achievements yet—keep going!
                        </p>
                    </div>
                ) : (
                    <div className="mt-6 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                        {data!.items.map((a) => (
                            <div
                                key={a.id}
                                className="rounded-md border border-gray-200 p-4"
                            >
                                <div className="flex items-start gap-3">
                                    <div className="mt-0.5 flex h-9 w-9 items-center justify-center rounded-md bg-[color:var(--light-aluminium)]">
                                        <TrophyIcon className="h-5 w-5 text-[color:var(--yellow)]" />
                                    </div>
                                    <div className="min-w-0">
                                        <p className="text-sm font-semibold text-gray-900">
                                            {a.title}
                                        </p>
                                        <p className="mt-1 text-sm text-gray-600">
                                            {a.description}
                                        </p>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </section>
    );
}
