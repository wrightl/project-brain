'use client';

import { getOnboardingStrings } from '@/_lib/onboarding-strings';
import type { SupportedLocale } from '@/_lib/locale';

interface AboutYouStepProps {
    formData: {
        onboarding?: {
            aboutYou?: {
                selfDescription?: string[];
                businessType?: string;
                proudMoment?: string;
                challenge?: string[];
            };
        };
    };
    updateFormData: (updates: any) => void;
    locale: SupportedLocale;
}

export default function AboutYouStep({
    formData,
    updateFormData,
    locale,
}: AboutYouStepProps) {
    const strings = getOnboardingStrings(locale);
    const aboutYouData = formData.onboarding?.aboutYou || {};
    const selectedDescriptions = aboutYouData.selfDescription || [];
    const selectedChallenges = aboutYouData.challenge || [];

    const handleToggleDescription = (value: string) => {
        const current = selectedDescriptions;
        const updated = current.includes(value)
            ? current.filter((v) => v !== value)
            : [...current, value];
        updateFormData({
            onboarding: {
                ...formData.onboarding,
                aboutYou: {
                    ...aboutYouData,
                    selfDescription: updated,
                },
            },
        });
    };

    const handleToggleChallenge = (value: string) => {
        const current = selectedChallenges;
        const updated = current.includes(value)
            ? current.filter((v) => v !== value)
            : [...current, value];
        updateFormData({
            onboarding: {
                ...formData.onboarding,
                aboutYou: {
                    ...aboutYouData,
                    challenge: updated,
                },
            },
        });
    };

    const handleChange = (
        e: React.ChangeEvent<HTMLTextAreaElement>
    ) => {
        const { name, value } = e.target;
        updateFormData({
            onboarding: {
                ...formData.onboarding,
                aboutYou: {
                    ...aboutYouData,
                    [name]: value,
                },
            },
        });
    };

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-2xl font-bold text-gray-900">
                    {strings.aboutYou.title}
                </h2>
                <p className="mt-1 text-sm text-gray-600">
                    {strings.aboutYou.description}
                </p>
            </div>

            <div className="space-y-6">
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                        {strings.aboutYou.selfDescriptionLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <div className="flex flex-wrap gap-2">
                        {strings.aboutYou.selfDescriptionOptions.map(
                            (option) => {
                                const isSelected = selectedDescriptions.includes(
                                    option.value
                                );
                                return (
                                    <button
                                        key={option.value}
                                        type="button"
                                        onClick={() =>
                                            handleToggleDescription(option.value)
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
                            }
                        )}
                    </div>
                </div>

                <div>
                    <label
                        htmlFor="businessType"
                        className="block text-sm font-medium text-gray-700"
                    >
                        {strings.aboutYou.businessTypeLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <textarea
                        id="businessType"
                        name="businessType"
                        rows={4}
                        value={aboutYouData.businessType || ''}
                        onChange={handleChange}
                        placeholder={strings.aboutYou.businessTypePlaceholder}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    />
                </div>

                <div>
                    <label
                        htmlFor="proudMoment"
                        className="block text-sm font-medium text-gray-700"
                    >
                        {strings.aboutYou.proudMomentLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <textarea
                        id="proudMoment"
                        name="proudMoment"
                        rows={4}
                        value={aboutYouData.proudMoment || ''}
                        onChange={handleChange}
                        placeholder={strings.aboutYou.proudMomentPlaceholder}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    />
                </div>

                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                        {strings.aboutYou.challengeLabel}{' '}
                        <span className="text-gray-500 text-xs">
                            {strings.common.optional}
                        </span>
                    </label>
                    <div className="flex flex-wrap gap-2">
                        {strings.aboutYou.challengeOptions.map((option) => {
                            const isSelected = selectedChallenges.includes(
                                option.value
                            );
                            return (
                                <button
                                    key={option.value}
                                    type="button"
                                    onClick={() =>
                                        handleToggleChallenge(option.value)
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
            </div>
        </div>
    );
}

