'use client';

import { getOnboardingStrings } from '@/_lib/onboarding-strings';
import type { SupportedLocale } from '@/_lib/locale';

interface CoachingBuddyStepProps {
    formData: {
        onboarding?: {
            coachingBuddy?: {
                tasks?: string[];
                communicationStyle?: string;
                toolsIntegration?: string;
                workingStyle?: string;
                additionalInfo?: string;
            };
        };
    };
    updateFormData: (updates: any) => void;
    locale: SupportedLocale;
}

export default function CoachingBuddyStep({
    formData,
    updateFormData,
    locale,
}: CoachingBuddyStepProps) {
    const strings = getOnboardingStrings(locale);
    const coachingBuddyData = formData.onboarding?.coachingBuddy || {};
    const selectedTasks = coachingBuddyData.tasks || [];

    const handleToggleTask = (value: string) => {
        const current = selectedTasks;
        const updated = current.includes(value)
            ? current.filter((v) => v !== value)
            : [...current, value];
        updateFormData({
            onboarding: {
                ...formData.onboarding,
                coachingBuddy: {
                    ...coachingBuddyData,
                    tasks: updated,
                },
            },
        });
    };

    const handleChange = (
        e: React.ChangeEvent<
            HTMLSelectElement | HTMLTextAreaElement
        >
    ) => {
        const { name, value } = e.target;
        updateFormData({
            onboarding: {
                ...formData.onboarding,
                coachingBuddy: {
                    ...coachingBuddyData,
                    [name]: value,
                },
            },
        });
    };

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-2xl font-bold text-gray-900">
                    {strings.coachingBuddy.title}
                </h2>
                <p className="mt-1 text-sm text-gray-600">
                    {strings.coachingBuddy.description}
                </p>
            </div>

            <div className="space-y-6">
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                        {strings.coachingBuddy.tasksLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <div className="flex flex-wrap gap-2">
                        {strings.coachingBuddy.tasksOptions.map((option) => {
                            const isSelected = selectedTasks.includes(
                                option.value
                            );
                            return (
                                <button
                                    key={option.value}
                                    type="button"
                                    onClick={() =>
                                        handleToggleTask(option.value)
                                    }
                                    className={`inline-flex items-center px-4 py-2 rounded-full text-sm font-medium transition-colors ${
                                        isSelected
                                            ? 'bg-indigo-600 text-white hover:bg-indigo-700'
                                            : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                                    }`}
                                >
                                    {option.label}
                                    {isSelected && (
                                        <svg
                                            className="ml-2 w-4 h-4"
                                            fill="none"
                                            stroke="currentColor"
                                            viewBox="0 0 24 24"
                                        >
                                            <path
                                                strokeLinecap="round"
                                                strokeLinejoin="round"
                                                strokeWidth={2}
                                                d="M5 13l4 4L19 7"
                                            />
                                        </svg>
                                    )}
                                </button>
                            );
                        })}
                    </div>
                </div>

                <div>
                    <label
                        htmlFor="communicationStyle"
                        className="block text-sm font-medium text-gray-700"
                    >
                        {strings.coachingBuddy.communicationStyleLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <select
                        id="communicationStyle"
                        name="communicationStyle"
                        value={coachingBuddyData.communicationStyle || ''}
                        onChange={handleChange}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    >
                        <option value="">
                            {strings.common.selectPlaceholder}
                        </option>
                        {strings.coachingBuddy.communicationStyleOptions.map(
                            (option) => (
                                <option key={option.value} value={option.value}>
                                    {option.label}
                                </option>
                            )
                        )}
                    </select>
                </div>

                <div>
                    <label
                        htmlFor="toolsIntegration"
                        className="block text-sm font-medium text-gray-700"
                    >
                        {strings.coachingBuddy.toolsIntegrationLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <textarea
                        id="toolsIntegration"
                        name="toolsIntegration"
                        rows={4}
                        value={coachingBuddyData.toolsIntegration || ''}
                        onChange={handleChange}
                        placeholder={
                            strings.coachingBuddy.toolsIntegrationPlaceholder
                        }
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    />
                </div>

                <div>
                    <label
                        htmlFor="workingStyle"
                        className="block text-sm font-medium text-gray-700"
                    >
                        {strings.coachingBuddy.workingStyleLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <textarea
                        id="workingStyle"
                        name="workingStyle"
                        rows={4}
                        value={coachingBuddyData.workingStyle || ''}
                        onChange={handleChange}
                        placeholder={strings.coachingBuddy.workingStylePlaceholder}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    />
                </div>

                <div>
                    <label
                        htmlFor="additionalInfo"
                        className="block text-sm font-medium text-gray-700"
                    >
                        {strings.coachingBuddy.additionalInfoLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <textarea
                        id="additionalInfo"
                        name="additionalInfo"
                        rows={4}
                        value={coachingBuddyData.additionalInfo || ''}
                        onChange={handleChange}
                        placeholder={
                            strings.coachingBuddy.additionalInfoPlaceholder
                        }
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    />
                </div>
            </div>
        </div>
    );
}

