'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { UserOnboardingData } from '@/_lib/types';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { useLocale, type SupportedLocale } from '@/_lib/locale';
import { getFollowOnCategories } from './user-wizard-steps/question-config';
import { getOnboardingStrings } from '@/_lib/onboarding-strings';
import BasicInfoStep from './user-wizard-steps/basic-info-step';
import NeurodiverseTraitsStep from './user-wizard-steps/neurodiverse-traits-step';
import WelcomeStep from './user-wizard-steps/welcome-step';
import AboutYouStep from './user-wizard-steps/about-you-step';
import PreferencesStep from './user-wizard-steps/preferences-step';
import ProfileStep from './user-wizard-steps/profile-step';
import CoachingBuddyStep from './user-wizard-steps/coaching-buddy-step';
import ClosingStep from './user-wizard-steps/closing-step';
import FollowOnQuestionsStep from './user-wizard-steps/follow-on-questions-step';

interface UserOnboardingWizardProps {
    userEmail: string;
}

type WizardStep =
    | 'basic'
    | 'neurodiverseTraits'
    | 'welcome'
    | 'aboutYou'
    | 'preferences'
    | 'profile'
    | 'coachingBuddy'
    | 'closing'
    | 'followOnQuestions';

export default function UserOnboardingWizard({
    userEmail,
}: UserOnboardingWizardProps) {
    const router = useRouter();
    const locale = useLocale();
    const strings = getOnboardingStrings(locale);

    const [currentStep, setCurrentStep] = useState<WizardStep>('basic');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [visitedSteps, setVisitedSteps] = useState<Set<WizardStep>>(
        new Set(['basic'])
    );

    const [formData, setFormData] = useState({
        email: userEmail,
        fullName: '',
        doB: '',
        preferredPronoun: '',
        neurodiverseTraits: [] as string[],
        preferences: '',
        onboarding: {
            locale: locale,
            welcome: {},
            aboutYou: {},
            preferences: {},
            profile: {},
            coachingBuddy: {},
            closing: {},
            followOnQuestions: {},
        },
    });

    // Determine which steps to show
    const getVisibleSteps = (): WizardStep[] => {
        const baseSteps: WizardStep[] = [
            'basic',
            'neurodiverseTraits',
            'welcome',
            'aboutYou',
            'preferences',
            'profile',
            'coachingBuddy',
            'closing',
        ];

        // Check if follow-on questions should be shown
        const followOnCategories = getFollowOnCategories({
            welcome: formData.onboarding.welcome || {},
            aboutYou: formData.onboarding.aboutYou || {},
            preferences: formData.onboarding.preferences || {},
            profile: formData.onboarding.profile || {},
            coachingBuddy: formData.onboarding.coachingBuddy || {},
        });

        if (followOnCategories.length > 0) {
            baseSteps.push('followOnQuestions');
        }

        return baseSteps;
    };

    const visibleSteps = getVisibleSteps();
    const currentStepIndex = visibleSteps.findIndex((s) => s === currentStep);
    const isFirstStep = currentStepIndex === 0;
    const isLastStep = currentStepIndex === visibleSteps.length - 1;

    // Update step titles based on locale
    const getStepTitle = (step: WizardStep): string => {
        switch (step) {
            case 'basic':
                return 'Basic Information';
            case 'neurodiverseTraits':
                return 'Neurodiverse Traits';
            case 'welcome':
                return strings.welcome.title;
            case 'aboutYou':
                return strings.aboutYou.title;
            case 'preferences':
                return strings.preferences.title;
            case 'profile':
                return strings.profile.title;
            case 'coachingBuddy':
                return strings.coachingBuddy.title;
            case 'closing':
                return strings.closing.title;
            case 'followOnQuestions':
                return 'Follow-up Questions';
            default:
                return '';
        }
    };

    const handleNext = () => {
        if (!isLastStep) {
            const nextStepIndex = currentStepIndex + 1;
            const nextStep = visibleSteps[nextStepIndex];
            setCurrentStep(nextStep);
            setVisitedSteps((prev) => new Set([...prev, nextStep]));
        }
    };

    const handleStepClick = (step: WizardStep, index: number) => {
        // Only allow clicking on steps that have been visited
        if (visitedSteps.has(step) && index !== currentStepIndex) {
            setCurrentStep(step);
        }
    };

    const handlePrevious = () => {
        if (!isFirstStep) {
            const prevStepIndex = currentStepIndex - 1;
            setCurrentStep(visibleSteps[prevStepIndex]);
        }
    };

    const handleSubmit = async () => {
        setError(null);
        setIsSubmitting(true);

        try {
            // Structure the onboarding data - always include locale
            const onboardingData: any = {
                locale: formData.onboarding.locale || locale,
            };

            // Only include sections that have data
            const welcome = formData.onboarding.welcome || {};
            if (Object.keys(welcome).length > 0) {
                onboardingData.welcome = welcome;
            }

            const aboutYou = formData.onboarding.aboutYou || {};
            if (Object.keys(aboutYou).length > 0) {
                onboardingData.aboutYou = aboutYou;
            }

            const prefs = formData.onboarding.preferences || {};
            if (Object.keys(prefs).length > 0) {
                onboardingData.preferences = prefs;
            }

            const profile = formData.onboarding.profile || {};
            if (Object.keys(profile).length > 0) {
                onboardingData.profile = profile;
            }

            const coachingBuddy = formData.onboarding.coachingBuddy || {};
            if (Object.keys(coachingBuddy).length > 0) {
                onboardingData.coachingBuddy = coachingBuddy;
            }

            const closing = formData.onboarding.closing || {};
            if (Object.keys(closing).length > 0) {
                onboardingData.closing = closing;
            }

            const followOn = formData.onboarding.followOnQuestions || {};
            if (Object.keys(followOn).length > 0) {
                onboardingData.followOnQuestions = followOn;
            }

            const data: UserOnboardingData = {
                email: formData.email,
                fullName: formData.fullName,
                doB: formData.doB,
                preferredPronoun: formData.preferredPronoun,
                neurodiverseTraits:
                    formData.neurodiverseTraits.length > 0
                        ? formData.neurodiverseTraits
                        : undefined,
                preferences: JSON.stringify({
                    ...(formData.preferences
                        ? { other: formData.preferences }
                        : {}),
                    onboarding: onboardingData,
                }),
                onboarding: onboardingData,
            };

            const response = await fetchWithAuth('/api/user/onboard', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(data),
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(
                    errorData.error || 'Failed to complete onboarding'
                );
            }

            router.push('/app');
            router.refresh();
        } catch (err) {
            setError(
                err instanceof Error
                    ? err.message
                    : 'Failed to complete onboarding'
            );
            setIsSubmitting(false);
        }
    };

    const updateFormData = (updates: any) => {
        setFormData((prev) => {
            // Deep merge for nested objects
            if (updates.onboarding) {
                return {
                    ...prev,
                    ...updates,
                    onboarding: {
                        ...prev.onboarding,
                        ...updates.onboarding,
                    },
                };
            }
            return { ...prev, ...updates };
        });
    };

    const canProceed = () => {
        switch (currentStep) {
            case 'basic':
                return (
                    formData.fullName.trim() !== '' &&
                    formData.doB !== '' &&
                    formData.preferredPronoun.trim() !== ''
                );
            case 'neurodiverseTraits':
            case 'welcome':
            case 'aboutYou':
            case 'preferences':
            case 'profile':
            case 'coachingBuddy':
            case 'closing':
            case 'followOnQuestions':
                return true; // All new steps are optional
            default:
                return false;
        }
    };

    return (
        <div className="space-y-6">
            {/* Progress Indicator */}
            <div className="bg-white rounded-lg shadow-sm p-4">
                <div className="flex items-center justify-between overflow-x-auto">
                    {visibleSteps.map((step, index) => {
                        const isCompleted =
                            visitedSteps.has(step) && index < currentStepIndex;
                        const isCurrent = index === currentStepIndex;
                        const isClickable =
                            visitedSteps.has(step) && !isCurrent;

                        return (
                            <div
                                key={step}
                                className="flex items-center flex-1 min-w-[100px]"
                            >
                                <div className="flex flex-col items-center flex-1">
                                    <button
                                        type="button"
                                        onClick={() =>
                                            handleStepClick(step, index)
                                        }
                                        disabled={!isClickable}
                                        className={`w-10 h-10 rounded-full flex items-center justify-center font-semibold text-sm transition-all ${
                                            isCompleted
                                                ? 'bg-indigo-600 text-white hover:bg-indigo-700 cursor-pointer'
                                                : isCurrent
                                                ? 'bg-indigo-600 text-white ring-4 ring-indigo-200 cursor-default'
                                                : isClickable
                                                ? 'bg-indigo-100 text-indigo-600 hover:bg-indigo-200 cursor-pointer'
                                                : 'bg-gray-200 text-gray-600'
                                        }`}
                                        title={
                                            isClickable
                                                ? `Go to ${getStepTitle(step)}`
                                                : undefined
                                        }
                                    >
                                        {isCompleted ? (
                                            <svg
                                                className="w-6 h-6"
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
                                        ) : (
                                            index + 1
                                        )}
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() =>
                                            handleStepClick(step, index)
                                        }
                                        disabled={!isClickable}
                                        className={`mt-2 text-xs font-medium text-center transition-colors ${
                                            isCurrent
                                                ? 'text-indigo-600'
                                                : isCompleted || isClickable
                                                ? 'text-gray-600 hover:text-indigo-600 cursor-pointer'
                                                : 'text-gray-400 cursor-not-allowed'
                                        }`}
                                        title={
                                            isClickable
                                                ? `Go to ${getStepTitle(step)}`
                                                : undefined
                                        }
                                    >
                                        {getStepTitle(step)}
                                    </button>
                                </div>
                                {index < visibleSteps.length - 1 && (
                                    <div
                                        className={`flex-1 h-1 mx-2 ${
                                            index < currentStepIndex
                                                ? 'bg-indigo-600'
                                                : 'bg-gray-200'
                                        }`}
                                    />
                                )}
                            </div>
                        );
                    })}
                </div>
            </div>

            {/* Error Message */}
            {error && (
                <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
                    {error}
                </div>
            )}

            {/* Step Content */}
            <div className="bg-white shadow rounded-lg p-8">
                {currentStep === 'basic' && (
                    <BasicInfoStep
                        formData={formData}
                        updateFormData={updateFormData}
                    />
                )}
                {currentStep === 'neurodiverseTraits' && (
                    <NeurodiverseTraitsStep
                        formData={formData}
                        updateFormData={updateFormData}
                    />
                )}
                {currentStep === 'welcome' && (
                    <WelcomeStep
                        formData={formData}
                        updateFormData={updateFormData}
                        locale={locale}
                    />
                )}
                {currentStep === 'aboutYou' && (
                    <AboutYouStep
                        formData={formData}
                        updateFormData={updateFormData}
                        locale={locale}
                    />
                )}
                {currentStep === 'preferences' && (
                    <PreferencesStep
                        formData={formData}
                        updateFormData={updateFormData}
                        locale={locale}
                    />
                )}
                {currentStep === 'profile' && (
                    <ProfileStep
                        formData={formData}
                        updateFormData={updateFormData}
                        locale={locale}
                    />
                )}
                {currentStep === 'coachingBuddy' && (
                    <CoachingBuddyStep
                        formData={formData}
                        updateFormData={updateFormData}
                        locale={locale}
                    />
                )}
                {currentStep === 'closing' && (
                    <ClosingStep
                        formData={formData}
                        updateFormData={updateFormData}
                        locale={locale}
                    />
                )}
                {currentStep === 'followOnQuestions' && (
                    <FollowOnQuestionsStep
                        formData={formData}
                        updateFormData={updateFormData}
                        locale={locale}
                    />
                )}
            </div>

            {/* Navigation Buttons */}
            <div className="flex justify-between items-center bg-white shadow rounded-lg p-4">
                <button
                    type="button"
                    onClick={handlePrevious}
                    disabled={isFirstStep || isSubmitting}
                    className="px-6 py-2 bg-gray-200 text-gray-700 font-medium rounded-md hover:bg-gray-300 disabled:bg-gray-100 disabled:text-gray-400 disabled:cursor-not-allowed transition-colors"
                >
                    {strings.common.previous}
                </button>

                <div className="text-sm text-gray-500">
                    Step {currentStepIndex + 1} of {visibleSteps.length}
                </div>

                {isLastStep ? (
                    <button
                        type="button"
                        onClick={handleSubmit}
                        disabled={!canProceed() || isSubmitting}
                        className="px-6 py-2 bg-indigo-600 text-white font-medium rounded-md hover:bg-indigo-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
                    >
                        {isSubmitting ? 'Saving...' : strings.common.complete}
                    </button>
                ) : (
                    <button
                        type="button"
                        onClick={handleNext}
                        disabled={!canProceed() || isSubmitting}
                        className="px-6 py-2 bg-indigo-600 text-white font-medium rounded-md hover:bg-indigo-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
                    >
                        {strings.common.next}
                    </button>
                )}
            </div>
        </div>
    );
}
