'use client';

import { getOnboardingStrings } from '@/_lib/onboarding-strings';
import type { SupportedLocale } from '@/_lib/locale';

interface WelcomeStepProps {
    formData: {
        onboarding?: {
            welcome?: {
                preferredName?: string;
                inspiration?: string;
                currentFeeling?: string;
            };
        };
    };
    updateFormData: (updates: any) => void;
    locale: SupportedLocale;
}

export default function WelcomeStep({
    formData,
    updateFormData,
    locale,
}: WelcomeStepProps) {
    const strings = getOnboardingStrings(locale);
    const welcomeData = formData.onboarding?.welcome || {};

    const handleChange = (
        e: React.ChangeEvent<
            HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
        >
    ) => {
        const { name, value } = e.target;
        updateFormData({
            onboarding: {
                ...formData.onboarding,
                welcome: {
                    ...welcomeData,
                    [name]: value,
                },
            },
        });
    };

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-2xl font-bold text-gray-900">
                    {strings.welcome.title}
                </h2>
                <p className="mt-1 text-sm text-gray-600">
                    {strings.welcome.description}
                </p>
            </div>

            <div className="space-y-6">
                <div>
                    <label
                        htmlFor="preferredName"
                        className="block text-sm font-medium text-gray-700"
                    >
                        {strings.welcome.preferredNameLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <input
                        type="text"
                        id="preferredName"
                        name="preferredName"
                        value={welcomeData.preferredName || ''}
                        onChange={handleChange}
                        placeholder={strings.welcome.preferredNamePlaceholder}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    />
                    <p className="mt-1 text-xs text-gray-500">
                        {strings.welcome.preferredNameHint}
                    </p>
                </div>

                <div>
                    <label
                        htmlFor="inspiration"
                        className="block text-sm font-medium text-gray-700"
                    >
                        {strings.welcome.inspirationLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <textarea
                        id="inspiration"
                        name="inspiration"
                        rows={4}
                        value={welcomeData.inspiration || ''}
                        onChange={handleChange}
                        placeholder={strings.welcome.inspirationPlaceholder}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    />
                </div>

                <div>
                    <label
                        htmlFor="currentFeeling"
                        className="block text-sm font-medium text-gray-700"
                    >
                        {strings.welcome.currentFeelingLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <select
                        id="currentFeeling"
                        name="currentFeeling"
                        value={welcomeData.currentFeeling || ''}
                        onChange={handleChange}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    >
                        <option value="">
                            {strings.common.selectPlaceholder}
                        </option>
                        {strings.welcome.currentFeelingOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                                {option.label}
                            </option>
                        ))}
                    </select>
                </div>
            </div>
        </div>
    );
}

