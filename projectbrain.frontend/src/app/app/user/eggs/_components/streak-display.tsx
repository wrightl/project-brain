'use client';

import { useCompletionStreak } from '@/_hooks/queries/use-goals';

export default function StreakDisplay() {
    const { data: streak, isLoading } = useCompletionStreak();

    // Only show if streak is greater than 1 (as per requirements)
    if (isLoading || !streak || streak <= 1) {
        return null;
    }

    return (
        <div className="bg-gradient-to-r from-yellow-400 to-orange-400 rounded-lg shadow-sm p-6 text-center">
            <div className="flex items-center justify-center space-x-2">
                <span className="text-3xl">🔥</span>
                <h2 className="text-2xl font-bold text-white">
                    {streak} Day Streak!
                </h2>
            </div>
            <p className="text-white/90 mt-2">
                Keep it up! You're on a roll!
            </p>
        </div>
    );
}

