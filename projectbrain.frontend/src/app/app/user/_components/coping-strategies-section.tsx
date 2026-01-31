'use client';

import { LightBulbIcon, SparklesIcon } from '@heroicons/react/24/outline';
import Link from 'next/link';
import { useCopingStrategyLibrary } from '@/_hooks/queries/use-coping-strategies';

function iconForKey(iconKey?: string) {
    switch (iconKey) {
        case 'sparkles':
            return SparklesIcon;
        case 'lightbulb':
            return LightBulbIcon;
        default:
            return LightBulbIcon;
    }
}

export default function CopingStrategiesSection() {
    const { data, isLoading } = useCopingStrategyLibrary();

    return (
        <section className="bg-white shadow rounded-lg p-6">
            <div className="flex items-start justify-between gap-4">
                <div>
                    <h2 className="text-xl font-semibold text-gray-900">
                        Coping strategies
                    </h2>
                    <p className="mt-1 text-sm text-gray-600">
                        Your saved strategies.
                    </p>
                </div>
                <div className="flex flex-col items-end gap-2">
                    <Link
                        href="/app/user/chat/strategies"
                        className="inline-flex items-center rounded-md bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700"
                    >
                        Get new strategies
                    </Link>
                    <Link
                        href="/app/user/strategies"
                        className="text-sm font-medium text-indigo-700 hover:text-indigo-900"
                    >
                        View library
                    </Link>
                </div>
            </div>

            {isLoading ? (
                <div className="mt-6 space-y-3">
                    <div className="h-16 bg-gray-100 rounded-md animate-pulse" />
                    <div className="h-16 bg-gray-100 rounded-md animate-pulse" />
                    <div className="h-16 bg-gray-100 rounded-md animate-pulse" />
                </div>
            ) : (data?.items?.length ?? 0) === 0 ? (
                <div className="mt-6 rounded-md border border-dashed border-gray-300 p-6">
                    <p className="text-sm text-gray-700">
                        You haven’t saved any coping strategies yet.
                    </p>
                </div>
            ) : (
                <div className="mt-6 space-y-3">
                    {data!.items.map((strategy) => {
                        const Icon = iconForKey(strategy.iconKey);
                        return (
                            <div
                                key={strategy.id}
                                className="flex items-start gap-4 rounded-md border border-gray-200 p-4"
                            >
                                <div className="flex items-start gap-3 min-w-0">
                                    <div className="mt-0.5 flex h-9 w-9 items-center justify-center rounded-md bg-indigo-50">
                                        <Icon className="h-5 w-5 text-indigo-700" />
                                    </div>
                                    <div className="min-w-0">
                                        <p className="text-sm font-semibold text-gray-900">
                                            {strategy.title}
                                        </p>
                                        <p className="mt-1 text-sm text-gray-600">
                                            {strategy.description}
                                        </p>
                                    </div>
                                </div>
                            </div>
                        );
                    })}
                </div>
            )}
        </section>
    );
}
