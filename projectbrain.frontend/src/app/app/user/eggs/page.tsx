'use client';

import {
    useTodaysGoals,
    useHasEverCreatedGoals,
} from '@/_hooks/queries/use-goals';
import { useRouter } from 'next/navigation';
import { useEffect } from 'react';
import EggsOnboardingPage from './_components/eggs-onboarding-page';
import GoalsListPage from './_components/goals-list-page';

export default function EggsPage() {
    const { data: goals, isLoading: goalsLoading } = useTodaysGoals();
    const { data: hasEverCreatedGoals, isLoading: historyLoading } =
        useHasEverCreatedGoals();
    const router = useRouter();
    const isLoading = goalsLoading || historyLoading;

    // Check if goals exist for today (with non-empty messages)
    const hasGoalsToday =
        goals && goals.some((goal) => goal.message.trim().length > 0);

    // Redirect to edit page if user has created goals before but has no goals today
    useEffect(() => {
        if (
            !isLoading &&
            hasEverCreatedGoals !== undefined &&
            hasEverCreatedGoals
        ) {
            if (!hasGoalsToday) {
                router.push('/app/user/eggs/edit');
            }
        }
    }, [isLoading, hasEverCreatedGoals, hasGoalsToday, router]);

    if (isLoading || hasEverCreatedGoals === undefined) {
        return (
            <div className="flex items-center justify-center min-h-screen">
                <div className="text-gray-600">Loading...</div>
            </div>
        );
    }

    // Show onboarding if user has never created goals
    if (!hasEverCreatedGoals) {
        return (
            <div className="min-h-screen bg-gray-50">
                <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                    <EggsOnboardingPage />
                </div>
            </div>
        );
    }

    // If no goals today, redirect to edit page
    if (!hasGoalsToday) {
        return null; // Will redirect via useEffect
    }

    return (
        <div className="min-h-screen bg-gray-50">
            <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                <GoalsListPage />
            </div>
        </div>
    );
}
