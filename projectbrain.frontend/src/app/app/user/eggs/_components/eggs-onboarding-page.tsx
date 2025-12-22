'use client';

import { CheckCircleIcon } from '@heroicons/react/24/outline';

interface OnboardingPageProps {
    onContinue: () => void;
}

export default function EggsOnboardingPage({
    onContinue,
}: OnboardingPageProps) {
    return (
        <div className="bg-white rounded-lg shadow-sm p-8 max-w-2xl mx-auto">
            <div className="text-center mb-8">
                <h1 className="text-3xl font-bold text-gray-900 mb-4">
                    Welcome to Daily Eggs!
                </h1>
                <p className="text-lg text-gray-600">
                    Set and track your daily goals to stay focused and
                    productive.
                </p>
            </div>

            <div className="space-y-6 mb-8">
                <div className="flex items-start">
                    <CheckCircleIcon className="h-6 w-6 text-green-500 mt-1 mr-3 flex-shrink-0" />
                    <div>
                        <h3 className="font-semibold text-gray-900 mb-1">
                            Set Up to 3 Daily Goals
                        </h3>
                        <p className="text-gray-600">
                            Define up to 3 goals you want to accomplish each
                            day. These can be anything from work tasks to
                            personal habits.
                        </p>
                    </div>
                </div>

                <div className="flex items-start">
                    <CheckCircleIcon className="h-6 w-6 text-green-500 mt-1 mr-3 flex-shrink-0" />
                    <div>
                        <h3 className="font-semibold text-gray-900 mb-1">
                            Track Your Progress
                        </h3>
                        <p className="text-gray-600">
                            Mark goals as complete as you finish them. Watch
                            your progress throughout the day and celebrate your
                            achievements!
                        </p>
                    </div>
                </div>

                <div className="flex items-start">
                    <CheckCircleIcon className="h-6 w-6 text-green-500 mt-1 mr-3 flex-shrink-0" />
                    <div>
                        <h3 className="font-semibold text-gray-900 mb-1">
                            Build Your Streak
                        </h3>
                        <p className="text-gray-600">
                            Complete all your goals each day to build a
                            completion streak. See how many days in a row you've
                            accomplished all your goals!
                        </p>
                    </div>
                </div>
            </div>

            <div className="text-center">
                <button
                    onClick={onContinue}
                    className="inline-flex items-center px-6 py-3 border border-transparent text-base font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                >
                    Get Started
                </button>
            </div>
        </div>
    );
}
