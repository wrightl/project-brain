'use client';

import Link from 'next/link';
import { useTodaysGoals } from '@/_hooks/queries/use-goals';
import { AcademicCapIcon } from '@heroicons/react/24/outline';
import { CheckCircleIcon as CheckCircleIconSolid } from '@heroicons/react/24/solid';

export default function EggsDashboardWidget() {
    const { data: goals, isLoading } = useTodaysGoals();

    if (isLoading) {
        return (
            <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-5">
                    <div className="animate-pulse">
                        <div className="h-4 bg-gray-200 rounded w-1/4 mb-2"></div>
                        <div className="h-8 bg-gray-200 rounded w-1/2"></div>
                    </div>
                </div>
            </div>
        );
    }

    // Filter out empty goals
    const activeGoals = goals?.filter(g => g.message.trim().length > 0) ?? [];
    const completedCount = activeGoals.filter(g => g.completed).length;
    const totalGoals = activeGoals.length;
    const percentage = totalGoals > 0 ? Math.round((completedCount / totalGoals) * 100) : 0;
    const allCompleted = totalGoals > 0 && completedCount === totalGoals;

    // If no goals set for today, show a prompt to set them
    if (totalGoals === 0) {
        return (
            <Link
                href="/app/eggs"
                className="bg-white overflow-hidden shadow rounded-lg hover:shadow-lg transition-shadow block"
            >
                <div className="p-5">
                    <div className="flex items-center">
                        <div className="flex-shrink-0">
                            <AcademicCapIcon
                                className="h-6 w-6 text-indigo-600"
                                aria-hidden="true"
                            />
                        </div>
                        <div className="ml-5 w-0 flex-1">
                            <dl>
                                <dt className="text-sm font-medium text-gray-500 truncate">
                                    Daily Eggs
                                </dt>
                                <dd className="text-lg font-semibold text-gray-900">
                                    Set your goals for today
                                </dd>
                            </dl>
                        </div>
                    </div>
                </div>
            </Link>
        );
    }

    return (
        <Link
            href="/app/eggs"
            className="bg-white overflow-hidden shadow rounded-lg hover:shadow-lg transition-shadow block"
        >
            <div className="p-5">
                <div className="flex items-center justify-between mb-3">
                    <div className="flex items-center">
                        <div className="flex-shrink-0">
                            <AcademicCapIcon
                                className={`h-6 w-6 ${allCompleted ? 'text-green-500' : 'text-indigo-600'}`}
                                aria-hidden="true"
                            />
                        </div>
                        <div className="ml-3">
                            <h3 className="text-sm font-medium text-gray-900">
                                Daily Eggs
                            </h3>
                        </div>
                    </div>
                    <div className="flex items-center space-x-1">
                        <span className={`text-sm font-semibold ${allCompleted ? 'text-green-600' : 'text-gray-900'}`}>
                            {completedCount}/{totalGoals}
                        </span>
                        {allCompleted && (
                            <CheckCircleIconSolid className="h-5 w-5 text-green-500" />
                        )}
                    </div>
                </div>

                {/* Progress Bar */}
                <div className="w-full bg-gray-200 rounded-full h-2 mb-2">
                    <div
                        className={`h-2 rounded-full transition-all duration-300 ${
                            allCompleted ? 'bg-green-500' : 'bg-indigo-600'
                        }`}
                        style={{ width: `${percentage}%` }}
                    />
                </div>

                {/* Goal List Preview */}
                <div className="mt-3 space-y-1">
                    {activeGoals.slice(0, 2).map((goal) => (
                        <div key={goal.id} className="flex items-center text-sm">
                            {goal.completed ? (
                                <CheckCircleIconSolid className="h-4 w-4 text-green-500 mr-2 flex-shrink-0" />
                            ) : (
                                <div className="h-4 w-4 border-2 border-gray-300 rounded mr-2 flex-shrink-0" />
                            )}
                            <span
                                className={`truncate ${
                                    goal.completed
                                        ? 'line-through text-gray-500'
                                        : 'text-gray-700'
                                }`}
                            >
                                {goal.message}
                            </span>
                        </div>
                    ))}
                    {activeGoals.length > 2 && (
                        <div className="text-xs text-gray-500 pt-1">
                            +{activeGoals.length - 2} more goal{activeGoals.length - 2 !== 1 ? 's' : ''}
                        </div>
                    )}
                </div>

                {allCompleted && (
                    <div className="mt-3 pt-3 border-t border-gray-200">
                        <p className="text-xs font-medium text-green-600">
                            🎉 All goals completed today!
                        </p>
                    </div>
                )}
            </div>
        </Link>
    );
}

