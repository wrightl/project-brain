'use client';

import { getOnboardingStrings } from '@/_lib/onboarding-strings';
import type { SupportedLocale } from '@/_lib/locale';

interface ClosingStepProps {
    formData: {
        onboarding?: {
            closing?: {
                safeSpace?: string;
                tipsOptIn?: boolean;
            };
        };
    };
    updateFormData: (updates: any) => void;
    locale: SupportedLocale;
}

export default function ClosingStep({
    formData,
    updateFormData,
    locale,
}: ClosingStepProps) {
    const strings = getOnboardingStrings(locale);
    const closingData = formData.onboarding?.closing || {};

    const handleChange = (
        e: React.ChangeEvent<HTMLTextAreaElement | HTMLInputElement>
    ) => {
        const { name, value, type } = e.target;
        const newValue = type === 'checkbox' ? (e.target as HTMLInputElement).checked : value;
        updateFormData({
            onboarding: {
                ...formData.onboarding,
                closing: {
                    ...closingData,
                    [name]: newValue,
                },
            },
        });
    };

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-2xl font-bold text-gray-900">
                    {strings.closing.title}
                </h2>
                <p className="mt-1 text-sm text-gray-600">
                    {strings.closing.description}
                </p>
            </div>

            <div className="space-y-6">
                <div>
                    <label
                        htmlFor="safeSpace"
                        className="block text-sm font-medium text-gray-700"
                    >
                        {strings.closing.safeSpaceLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <textarea
                        id="safeSpace"
                        name="safeSpace"
                        rows={4}
                        value={closingData.safeSpace || ''}
                        onChange={handleChange}
                        placeholder={strings.closing.safeSpacePlaceholder}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    />
                </div>

                <div className="flex items-start">
                    <div className="flex items-center h-5">
                        <input
                            id="tipsOptIn"
                            name="tipsOptIn"
                            type="checkbox"
                            checked={closingData.tipsOptIn || false}
                            onChange={handleChange}
                            className="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300 rounded"
                        />
                    </div>
                    <div className="ml-3 text-sm">
                        <label
                            htmlFor="tipsOptIn"
                            className="font-medium text-gray-700"
                        >
                            {strings.closing.tipsOptInLabel}
                        </label>
                    </div>
                </div>
            </div>
        </div>
    );
}

