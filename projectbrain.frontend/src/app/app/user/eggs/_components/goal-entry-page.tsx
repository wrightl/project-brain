'use client';

import { useState } from 'react';
import { useCreateOrUpdateGoals } from '@/_hooks/queries/use-goals';
import { useRouter } from 'next/navigation';
import toast from 'react-hot-toast';
import { Input } from '@headlessui/react';

interface GoalEntryPageProps {
    initialGoals?: string[];
    onGoalsSaved?: () => void;
}

export default function GoalEntryPage({
    initialGoals = [],
    onGoalsSaved,
}: GoalEntryPageProps) {
    const router = useRouter();
    // Pre-populate with initial goals, pad to 3 with empty strings
    const [goals, setGoals] = useState<string[]>(() => {
        const padded = [...initialGoals];
        while (padded.length < 3) {
            padded.push('');
        }
        return padded.slice(0, 3);
    });
    const [errors, setErrors] = useState<string[]>(['', '', '']);
    const createOrUpdateMutation = useCreateOrUpdateGoals();

    const handleGoalChange = (index: number, value: string) => {
        const newGoals = [...goals];
        newGoals[index] = value;
        setGoals(newGoals);

        // Clear error for this field
        const newErrors = [...errors];
        if (value.length > 500) {
            newErrors[index] = 'Goal must be 500 characters or less';
        } else {
            newErrors[index] = '';
        }
        setErrors(newErrors);
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        // Validate: at least one goal must be provided
        const nonEmptyGoals = goals.filter((g) => g.trim().length > 0);
        if (nonEmptyGoals.length === 0) {
            toast.error('Please enter at least one goal');
            return;
        }

        // Validate: no goals exceed 500 characters
        const hasErrors = errors.some((e) => e.length > 0);
        if (hasErrors) {
            toast.error('Please fix validation errors');
            return;
        }

        try {
            await createOrUpdateMutation.mutateAsync(nonEmptyGoals);
            toast.success('Goals saved successfully!');
            // Call the callback if provided, otherwise navigate to main page
            if (onGoalsSaved) {
                onGoalsSaved();
            } else {
                router.push('/app/user/eggs');
            }
        } catch (error) {
            toast.error(
                error instanceof Error ? error.message : 'Failed to save goals'
            );
        }
    };

    return (
        <div className="bg-white rounded-lg shadow-sm p-8 max-w-2xl mx-auto">
            <div className="mb-8">
                <h1 className="text-3xl font-bold text-gray-900 mb-2">
                    {initialGoals.length > 0
                        ? 'Edit Your Daily Goals'
                        : 'Set Your Daily Goals'}
                </h1>
                <p className="text-gray-600">
                    {initialGoals.length > 0
                        ? 'Update your goals for today. You can modify any of them.'
                        : "Enter up to 3 goals you want to accomplish today. You can leave slots empty if you don't need all 3."}
                </p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-6">
                {[0, 1, 2].map((index) => (
                    <div key={index}>
                        <label
                            htmlFor={`goal-${index}`}
                            className="block text-sm font-medium text-gray-700 mb-2"
                        >
                            Goal {index + 1}
                        </label>
                        <Input
                            id={`goal-${index}`}
                            // rows={3}
                            value={goals[index]}
                            onChange={(e) =>
                                handleGoalChange(index, e.target.value)
                            }
                            className={`w-full px-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 ${
                                errors[index]
                                    ? 'border-red-300'
                                    : 'border-gray-300'
                            }`}
                            placeholder={`Enter goal ${index + 1}...`}
                            maxLength={500}
                        />
                        <div className="mt-1 flex justify-between">
                            {errors[index] && (
                                <p className="text-sm text-red-600">
                                    {errors[index]}
                                </p>
                            )}
                            <p className="text-sm text-gray-500 ml-auto">
                                {goals[index].length}/500 characters
                            </p>
                        </div>
                    </div>
                ))}

                <div className="flex justify-end space-x-4 pt-4">
                    <button
                        type="submit"
                        disabled={createOrUpdateMutation.isPending}
                        className="inline-flex items-center px-6 py-3 border border-transparent text-base font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                        {createOrUpdateMutation.isPending
                            ? 'Saving...'
                            : 'Save Goals'}
                    </button>
                </div>
            </form>
        </div>
    );
}
