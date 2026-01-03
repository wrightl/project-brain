'use client';

import { getOnboardingStrings } from '@/_lib/onboarding-strings';
import type { SupportedLocale } from '@/_lib/locale';

interface ProfileStepProps {
    formData: {
        onboarding?: {
            profile?: {
                strengths?: string[];
                supportAreas?: string[];
                motivationStyle?: string;
                neurodivergentUnderstanding?: string;
                biggestGoal?: string;
            };
        };
    };
    updateFormData: (updates: any) => void;
    locale: SupportedLocale;
}

export default function ProfileStep({
    formData,
    updateFormData,
    locale,
}: ProfileStepProps) {
    const strings = getOnboardingStrings(locale);
    const profileData = formData.onboarding?.profile || {};
    const selectedStrengths = profileData.strengths || [];
    const selectedSupportAreas = profileData.supportAreas || [];

    const handleToggleStrength = (value: string) => {
        const current = selectedStrengths;
        const updated = current.includes(value)
            ? current.filter((v) => v !== value)
            : [...current, value];
        updateFormData({
            onboarding: {
                ...formData.onboarding,
                profile: {
                    ...profileData,
                    strengths: updated,
                },
            },
        });
    };

    const handleToggleSupportArea = (value: string) => {
        const current = selectedSupportAreas;
        const updated = current.includes(value)
            ? current.filter((v) => v !== value)
            : [...current, value];
        updateFormData({
            onboarding: {
                ...formData.onboarding,
                profile: {
                    ...profileData,
                    supportAreas: updated,
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
                profile: {
                    ...profileData,
                    [name]: value,
                },
            },
        });
    };

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-2xl font-bold text-gray-900">
                    {strings.profile.title}
                </h2>
                <p className="mt-1 text-sm text-gray-600">
                    {strings.profile.description}
                </p>
            </div>

            <div className="space-y-6">
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                        {strings.profile.strengthsLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <div className="flex flex-wrap gap-2">
                        {strings.profile.strengthsOptions.map((option) => {
                            const isSelected = selectedStrengths.includes(
                                option.value
                            );
                            return (
                                <button
                                    key={option.value}
                                    type="button"
                                    onClick={() =>
                                        handleToggleStrength(option.value)
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
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                        {strings.profile.supportAreasLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <div className="flex flex-wrap gap-2">
                        {strings.profile.supportAreasOptions.map((option) => {
                            const isSelected = selectedSupportAreas.includes(
                                option.value
                            );
                            return (
                                <button
                                    key={option.value}
                                    type="button"
                                    onClick={() =>
                                        handleToggleSupportArea(option.value)
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
                        htmlFor="motivationStyle"
                        className="block text-sm font-medium text-gray-700"
                    >
                        {strings.profile.motivationStyleLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <select
                        id="motivationStyle"
                        name="motivationStyle"
                        value={profileData.motivationStyle || ''}
                        onChange={handleChange}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    >
                        <option value="">
                            {strings.common.selectPlaceholder}
                        </option>
                        {strings.profile.motivationStyleOptions.map(
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
                        htmlFor="neurodivergentUnderstanding"
                        className="block text-sm font-medium text-gray-700"
                    >
                        {strings.profile.neurodivergentUnderstandingLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <textarea
                        id="neurodivergentUnderstanding"
                        name="neurodivergentUnderstanding"
                        rows={4}
                        value={profileData.neurodivergentUnderstanding || ''}
                        onChange={handleChange}
                        placeholder={
                            strings.profile.neurodivergentUnderstandingPlaceholder
                        }
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    />
                </div>

                <div>
                    <label
                        htmlFor="biggestGoal"
                        className="block text-sm font-medium text-gray-700"
                    >
                        {strings.profile.biggestGoalLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <textarea
                        id="biggestGoal"
                        name="biggestGoal"
                        rows={4}
                        value={profileData.biggestGoal || ''}
                        onChange={handleChange}
                        placeholder={strings.profile.biggestGoalPlaceholder}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    />
                </div>
            </div>
        </div>
    );
}

