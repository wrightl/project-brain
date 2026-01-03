'use client';

import { getOnboardingStrings } from '@/_lib/onboarding-strings';
import type { SupportedLocale } from '@/_lib/locale';

interface PreferencesStepProps {
    formData: {
        preferences?: string;
        onboarding?: {
            preferences?: {
                learningStyle?: string;
                informationDepth?: string;
                celebrationStyle?: string;
            };
        };
    };
    updateFormData: (updates: any) => void;
    locale: SupportedLocale;
}

export default function PreferencesStep({
    formData,
    updateFormData,
    locale,
}: PreferencesStepProps) {
    const strings = getOnboardingStrings(locale);
    const preferencesData = formData.onboarding?.preferences || {};

    const handleChange = (
        e: React.ChangeEvent<HTMLSelectElement>
    ) => {
        const { name, value } = e.target;
        updateFormData({
            onboarding: {
                ...formData.onboarding,
                preferences: {
                    ...preferencesData,
                    [name]: value,
                },
            },
        });
    };

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-2xl font-bold text-gray-900">
                    {strings.preferences.title}
                </h2>
                <p className="mt-1 text-sm text-gray-600">
                    {strings.preferences.description}
                </p>
            </div>

            <div className="space-y-6">
                <div>
                    <label
                        htmlFor="learningStyle"
                        className="block text-sm font-medium text-gray-700"
                    >
                        {strings.preferences.learningStyleLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <select
                        id="learningStyle"
                        name="learningStyle"
                        value={preferencesData.learningStyle || ''}
                        onChange={handleChange}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    >
                        <option value="">
                            {strings.common.selectPlaceholder}
                        </option>
                        {strings.preferences.learningStyleOptions.map(
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
                        htmlFor="informationDepth"
                        className="block text-sm font-medium text-gray-700"
                    >
                        {strings.preferences.informationDepthLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <select
                        id="informationDepth"
                        name="informationDepth"
                        value={preferencesData.informationDepth || ''}
                        onChange={handleChange}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    >
                        <option value="">
                            {strings.common.selectPlaceholder}
                        </option>
                        {strings.preferences.informationDepthOptions.map(
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
                        htmlFor="celebrationStyle"
                        className="block text-sm font-medium text-gray-700"
                    >
                        {strings.preferences.celebrationStyleLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <select
                        id="celebrationStyle"
                        name="celebrationStyle"
                        value={preferencesData.celebrationStyle || ''}
                        onChange={handleChange}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    >
                        <option value="">
                            {strings.common.selectPlaceholder}
                        </option>
                        {strings.preferences.celebrationStyleOptions.map(
                            (option) => (
                                <option key={option.value} value={option.value}>
                                    {option.label}
                                </option>
                            )
                        )}
                    </select>
                </div>
            </div>
        </div>
    );
}
