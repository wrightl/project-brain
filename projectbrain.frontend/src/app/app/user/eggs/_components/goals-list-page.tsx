'use client';

import { useState, useEffect } from 'react';
import { useTodaysGoals, useCompleteGoal } from '@/_hooks/queries/use-goals';
import { useRouter } from 'next/navigation';
import { PencilIcon } from '@heroicons/react/24/outline';
import ProgressIndicator from './progress-indicator';
import StreakDisplay from './streak-display';
import CompletionOverlay from './completion-overlay';
import { Goal } from '@/_services/goal-service';

interface GoalsListPageProps {
    onEdit?: () => void;
}

export default function GoalsListPage({ onEdit }: GoalsListPageProps) {
    const { data: goals, isLoading } = useTodaysGoals();
    const completeGoalMutation = useCompleteGoal();
    const router = useRouter();
    const [showCompletionOverlay, setShowCompletionOverlay] = useState(false);
    const [completedGoalMessage, setCompletedGoalMessage] =
        useState<string>('');
    const [allCompletedShown, setAllCompletedShown] = useState(false);

    // Check if all goals are completed
    const completedCount =
        goals?.filter((g) => g.completed && g.message.trim().length > 0)
            .length ?? 0;
    const totalGoals =
        goals?.filter((g) => g.message.trim().length > 0).length ?? 0;
    const allCompleted = totalGoals > 0 && completedCount === totalGoals;

    // Navigate to all-completed page when all goals are done
    useEffect(() => {
        if (allCompleted && !allCompletedShown && goals) {
            setAllCompletedShown(true);
            // Small delay to show completion overlay first
            setTimeout(() => {
                router.push('/app/user/eggs/all-completed');
            }, 2000);
        }
    }, [allCompleted, allCompletedShown, goals, router]);

    const handleToggleComplete = async (
        index: number,
        currentCompleted: boolean,
        goalMessage: string
    ) => {
        try {
            const newCompleted = !currentCompleted;
            await completeGoalMutation.mutateAsync({
                index,
                completed: newCompleted,
            });

            // Show completion overlay if goal was just completed
            if (newCompleted && goalMessage.trim().length > 0) {
                setCompletedGoalMessage(goalMessage);
                setShowCompletionOverlay(true);
            }
        } catch (error) {
            console.error('Failed to update goal:', error);
        }
    };

    if (isLoading) {
        return (
            <div className="flex items-center justify-center min-h-[400px]">
                <div className="text-gray-600">Loading goals...</div>
            </div>
        );
    }

    if (!goals || goals.length === 0) {
        return (
            <div className="text-center py-12">
                <p className="text-gray-600">
                    No goals found. Please create your goals for today.
                </p>
            </div>
        );
    }

    // Filter out empty goals for display
    const displayGoals = goals.filter((g) => g.message.trim().length > 0);

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-3xl font-bold text-gray-900 mb-2">
                        Your Daily Goals
                    </h1>
                    <p className="text-gray-600">
                        Track your progress and mark goals as complete as you
                        finish them.
                    </p>
                </div>
                {onEdit && (
                    <button
                        onClick={onEdit}
                        className="inline-flex items-center px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                    >
                        <PencilIcon className="h-4 w-4 mr-2" />
                        Edit Goals
                    </button>
                )}
            </div>

            <div className="bg-white rounded-lg shadow-sm p-6">
                <div className="space-y-4">
                    {displayGoals.map((goal) => (
                        <div
                            key={goal.id}
                            className={`flex items-start p-4 rounded-lg border-2 ${
                                goal.completed
                                    ? 'bg-green-50 border-green-200'
                                    : 'bg-white border-gray-200'
                            }`}
                        >
                            <input
                                type="checkbox"
                                checked={goal.completed}
                                onChange={() =>
                                    handleToggleComplete(
                                        goal.index,
                                        goal.completed,
                                        goal.message
                                    )
                                }
                                className="mt-1 h-5 w-5 text-blue-600 focus:ring-blue-500 border-gray-300 rounded cursor-pointer"
                                disabled={completeGoalMutation.isPending}
                            />
                            <label className="ml-4 flex-1 cursor-pointer">
                                <span
                                    className={`text-lg ${
                                        goal.completed
                                            ? 'line-through text-gray-500'
                                            : 'text-gray-900'
                                    }`}
                                >
                                    {goal.message}
                                </span>
                                {goal.completed && goal.completedAt && (
                                    <p className="text-sm text-gray-500 mt-1">
                                        Completed at{' '}
                                        {new Date(
                                            goal.completedAt
                                        ).toLocaleTimeString()}
                                    </p>
                                )}
                            </label>
                        </div>
                    ))}
                </div>
            </div>

            <StreakDisplay />

            <ProgressIndicator completed={completedCount} total={totalGoals} />

            <CompletionOverlay
                isOpen={showCompletionOverlay}
                onClose={() => setShowCompletionOverlay(false)}
                goalMessage={completedGoalMessage}
            />
        </div>
    );
}
