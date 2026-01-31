'use client';

import Link from 'next/link';
import { CheckCircleIcon, PencilSquareIcon } from '@heroicons/react/24/outline';
import { CheckCircleIcon as CheckCircleIconSolid } from '@heroicons/react/24/solid';
import { useCompleteGoal, useTodaysGoals } from '@/_hooks/queries/use-goals';

export default function TodaysGoalsSection() {
    const { data: goals, isLoading } = useTodaysGoals();
    const completeGoal = useCompleteGoal();

    const totalGoals = 3;
    const completedCount = (goals ?? []).filter((g) => g.completed).length;
    const allEmpty =
        (goals ?? []).length > 0 &&
        (goals ?? []).every((g) => g.message.trim().length === 0);

    const onToggle = async (index: number, completed: boolean) => {
        await completeGoal.mutateAsync({ index, completed });
    };

    return (
        <section className="bg-white shadow rounded-lg p-6">
            <div className="flex items-start justify-between gap-4">
                <div>
                    <h2 className="text-xl font-semibold text-gray-900">
                        Today’s goals
                    </h2>
                    <p className="mt-1 text-sm text-gray-600">
                        {isLoading ? (
                            'Loading…'
                        ) : (
                            <>
                                <span className="font-semibold text-gray-900">
                                    {completedCount}
                                </span>{' '}
                                of{' '}
                                <span className="font-semibold text-gray-900">
                                    {totalGoals}
                                </span>{' '}
                                completed
                            </>
                        )}
                    </p>
                </div>

                <Link
                    href="/app/user/eggs/edit"
                    className="inline-flex items-center gap-2 text-sm font-medium text-indigo-600 hover:text-indigo-700"
                >
                    <PencilSquareIcon className="h-5 w-5" />
                    Edit goals
                </Link>
            </div>

            {isLoading ? (
                <div className="mt-6 space-y-3">
                    <div className="h-14 bg-gray-100 rounded-md animate-pulse" />
                    <div className="h-14 bg-gray-100 rounded-md animate-pulse" />
                    <div className="h-14 bg-gray-100 rounded-md animate-pulse" />
                </div>
            ) : allEmpty ? (
                <div className="mt-6 rounded-md border border-dashed border-gray-300 p-6">
                    <p className="text-sm text-gray-700">
                        You haven’t set your goals for today yet.
                    </p>
                    <Link
                        href="/app/user/eggs/edit"
                        className="mt-3 inline-flex items-center gap-2 rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
                    >
                        <PencilSquareIcon className="h-5 w-5" />
                        Set today’s goals
                    </Link>
                </div>
            ) : (
                <div className="mt-6 space-y-3">
                    {(goals ?? []).slice(0, 3).map((goal) => {
                        const isEmpty = goal.message.trim().length === 0;
                        const isDisabled = isEmpty || completeGoal.isPending;

                        return (
                            <div
                                key={goal.id}
                                className={`flex items-center justify-between gap-4 rounded-md border p-4 ${
                                    goal.completed
                                        ? 'border-green-200 bg-green-50'
                                        : 'border-gray-200 bg-white'
                                }`}
                            >
                                <div className="min-w-0">
                                    <p
                                        className={`text-sm font-medium ${
                                            isEmpty
                                                ? 'text-gray-500 italic'
                                                : goal.completed
                                                  ? 'text-gray-700 line-through'
                                                  : 'text-gray-900'
                                        }`}
                                    >
                                        {isEmpty
                                            ? 'Goal not set'
                                            : goal.message}
                                    </p>
                                    {isEmpty && (
                                        <p className="mt-1 text-xs text-gray-500">
                                            Tap “Edit goals” to add this.
                                        </p>
                                    )}
                                </div>

                                <button
                                    type="button"
                                    disabled={isDisabled}
                                    onClick={() =>
                                        onToggle(goal.index, !goal.completed)
                                    }
                                    className={`inline-flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                                        isDisabled
                                            ? 'bg-gray-100 text-gray-400 cursor-not-allowed'
                                            : goal.completed
                                              ? 'bg-white text-green-700 hover:bg-green-100'
                                              : 'bg-indigo-600 text-white hover:bg-indigo-700'
                                    }`}
                                    aria-pressed={goal.completed}
                                >
                                    {goal.completed ? (
                                        <>
                                            <CheckCircleIconSolid className="h-5 w-5" />
                                            Mark incomplete
                                        </>
                                    ) : (
                                        <>
                                            <CheckCircleIcon className="h-5 w-5" />
                                            Mark complete
                                        </>
                                    )}
                                </button>
                            </div>
                        );
                    })}
                </div>
            )}
        </section>
    );
}
