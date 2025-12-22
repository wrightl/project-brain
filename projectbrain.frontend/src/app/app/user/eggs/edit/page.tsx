'use client';

import { useTodaysGoals } from '@/_hooks/queries/use-goals';
import GoalEntryPage from '../_components/goal-entry-page';
import { useRouter } from 'next/navigation';

export default function EditGoalsPage() {
    const { data: goals, isLoading } = useTodaysGoals();
    const router = useRouter();

    if (isLoading) {
        return (
            <div className="flex items-center justify-center min-h-screen">
                <div className="text-gray-600">Loading...</div>
            </div>
        );
    }

    const initialGoals =
        goals
            ?.map((g) => g.message)
            .filter((m) => m.trim().length > 0) ?? [];

    return (
        <div className="min-h-screen bg-gray-50">
            <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                <GoalEntryPage
                    initialGoals={initialGoals}
                    onGoalsSaved={() => router.push('/app/user/eggs')}
                />
            </div>
        </div>
    );
}

