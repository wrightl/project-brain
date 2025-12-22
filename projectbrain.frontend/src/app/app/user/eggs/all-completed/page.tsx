'use client';

import { useRouter } from 'next/navigation';
import StreakDisplay from '../_components/streak-display';
import { useTodaysGoals } from '@/_hooks/queries/use-goals';

export default function AllGoalsCompletedPage() {
    const router = useRouter();
    const { data: goals } = useTodaysGoals();

    const handleGoBack = () => {
        router.push('/app/user/eggs');
    };

    return (
        <div className="min-h-screen bg-gray-50 flex items-center justify-center px-4">
            <div className="bg-white rounded-lg shadow-lg p-12 max-w-2xl w-full text-center">
                <div className="mb-8">
                    <div className="text-6xl mb-4">🎉</div>
                    <h1 className="text-4xl font-bold text-gray-900 mb-4">
                        Congratulations!
                    </h1>
                    <p className="text-xl text-gray-600 mb-2">
                        You've completed all your goals for today!
                    </p>
                    <p className="text-lg text-gray-500">
                        You're doing amazing! Keep up the great work.
                    </p>
                </div>

                <div className="mb-8">
                    <StreakDisplay />
                </div>

                <div className="space-y-4">
                    <button
                        onClick={handleGoBack}
                        className="inline-flex items-center px-6 py-3 border border-transparent text-base font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                    >
                        View My Goals
                    </button>
                    <p className="text-sm text-gray-500">
                        Come back tomorrow to set new goals!
                    </p>
                </div>
            </div>
        </div>
    );
}
