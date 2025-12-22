'use client';

import {
    useTodaysGoals,
    useHasEverCreatedGoals,
} from '@/_hooks/queries/use-goals';
import EggsOnboardingPage from './_components/eggs-onboarding-page';
import GoalEntryPage from './_components/goal-entry-page';
import GoalsListPage from './_components/goals-list-page';
import { useState } from 'react';

export default function EggsPage() {
    const { data: goals, isLoading: goalsLoading } = useTodaysGoals();
    const { data: hasEverCreatedGoals, isLoading: historyLoading } =
        useHasEverCreatedGoals();
    const isLoading = goalsLoading || historyLoading;
    const [showGoalEntry, setShowGoalEntry] = useState(false);

    if (isLoading || hasEverCreatedGoals === undefined) {
        return (
            <div className="flex items-center justify-center min-h-screen">
                <div className="text-gray-600">Loading...</div>
            </div>
        );
    }

    // Show onboarding if user has never created goals and hasn't chosen to continue
    if (!hasEverCreatedGoals && !showGoalEntry) {
        return (
            <div className="min-h-screen bg-gray-50">
                <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                    <EggsOnboardingPage
                        onContinue={() => setShowGoalEntry(true)}
                    />
                </div>
            </div>
        );
    }

    // Check if goals exist for today (with non-empty messages)
    const hasGoalsToday =
        goals && goals.some((goal) => goal.message.trim().length > 0);

    // If user has clicked continue from onboarding or has no goals today, show entry page
    if (!hasGoalsToday || showGoalEntry) {
        return (
            <div className="min-h-screen bg-gray-50">
                <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                    <GoalEntryPage
                        initialGoals={
                            goals
                                ?.map((g) => g.message)
                                .filter((m) => m.trim().length > 0) ?? []
                        }
                        onGoalsSaved={() => setShowGoalEntry(false)}
                    />
                </div>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gray-50">
            <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                <GoalsListPage onEdit={() => setShowGoalEntry(true)} />
            </div>
        </div>
    );
}
