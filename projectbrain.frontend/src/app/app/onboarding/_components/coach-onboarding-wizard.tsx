'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { CoachOnboardingData } from '@/_lib/types';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import BasicInfoStep from './coach-wizard-steps/basic-info-step';
import QualificationsStep from './coach-wizard-steps/qualifications-step';
import SpecialismsStep from './coach-wizard-steps/specialisms-step';
import AgeGroupsStep from './coach-wizard-steps/age-groups-step';

interface CoachOnboardingWizardProps {
    userEmail: string;
}

type WizardStep = 'basic' | 'qualifications' | 'specialisms' | 'ageGroups';

const STEPS: { key: WizardStep; title: string }[] = [
    { key: 'basic', title: 'Basic Information' },
    { key: 'qualifications', title: 'Qualifications' },
    { key: 'specialisms', title: 'Specialisms' },
    { key: 'ageGroups', title: 'Age Groups' },
];

export default function CoachOnboardingWizard({
    userEmail,
}: CoachOnboardingWizardProps) {
    const router = useRouter();
    const [currentStep, setCurrentStep] = useState<WizardStep>('basic');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [formData, setFormData] = useState({
        email: userEmail,
        fullName: '',
        streetAddress: '',
        addressLine2: '',
        city: '',
        stateProvince: '',
        postalCode: '',
        country: '',
        qualifications: [] as string[],
        specialisms: [] as string[],
        ageGroups: [] as string[],
    });

    const currentStepIndex = STEPS.findIndex((s) => s.key === currentStep);
    const isFirstStep = currentStepIndex === 0;
    const isLastStep = currentStepIndex === STEPS.length - 1;

    const handleNext = () => {
        if (!isLastStep) {
            const nextStepIndex = currentStepIndex + 1;
            setCurrentStep(STEPS[nextStepIndex].key);
        }
    };

    const handlePrevious = () => {
        if (!isFirstStep) {
            const prevStepIndex = currentStepIndex - 1;
            setCurrentStep(STEPS[prevStepIndex].key);
        }
    };

    const handleSubmit = async () => {
        setError(null);
        setIsSubmitting(true);

        try {
            const data: CoachOnboardingData = {
                email: formData.email,
                fullName: formData.fullName,
                streetAddress: formData.streetAddress || undefined,
                addressLine2: formData.addressLine2 || undefined,
                city: formData.city || undefined,
                stateProvince: formData.stateProvince || undefined,
                postalCode: formData.postalCode || undefined,
                country: formData.country || undefined,
                qualifications: formData.qualifications,
                specialisms: formData.specialisms,
                ageGroups: formData.ageGroups,
            };

            console.log(`Onboarding data ${JSON.stringify(data)}`);

            const response = await fetchWithAuth('/api/coach/onboard', {
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

    const updateFormData = (updates: Partial<typeof formData>) => {
        setFormData((prev) => ({ ...prev, ...updates }));
    };

    const canProceed = () => {
        switch (currentStep) {
            case 'basic':
                return formData.fullName.trim() !== '';
            case 'qualifications':
            case 'specialisms':
            case 'ageGroups':
                return true; // These steps are optional
            default:
                return false;
        }
    };

    return (
        <div className="space-y-6">
            {/* Progress Indicator */}
            <div className="bg-white rounded-lg shadow-sm p-4">
                <div className="flex items-center justify-between">
                    {STEPS.map((step, index) => (
                        <div
                            key={step.key}
                            className="flex items-center flex-1"
                        >
                            <div className="flex flex-col items-center flex-1">
                                <div
                                    className={`w-10 h-10 rounded-full flex items-center justify-center font-semibold text-sm ${
                                        index < currentStepIndex
                                            ? 'bg-indigo-600 text-white'
                                            : index === currentStepIndex
                                            ? 'bg-indigo-600 text-white ring-4 ring-indigo-200'
                                            : 'bg-gray-200 text-gray-600'
                                    }`}
                                >
                                    {index < currentStepIndex ? (
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
                                </div>
                                <span
                                    className={`mt-2 text-xs font-medium ${
                                        index === currentStepIndex
                                            ? 'text-indigo-600'
                                            : index < currentStepIndex
                                            ? 'text-gray-600'
                                            : 'text-gray-400'
                                    }`}
                                >
                                    {step.title}
                                </span>
                            </div>
                            {index < STEPS.length - 1 && (
                                <div
                                    className={`flex-1 h-1 mx-2 ${
                                        index < currentStepIndex
                                            ? 'bg-indigo-600'
                                            : 'bg-gray-200'
                                    }`}
                                />
                            )}
                        </div>
                    ))}
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
                {currentStep === 'qualifications' && (
                    <QualificationsStep
                        formData={formData}
                        updateFormData={updateFormData}
                    />
                )}
                {currentStep === 'specialisms' && (
                    <SpecialismsStep
                        formData={formData}
                        updateFormData={updateFormData}
                    />
                )}
                {currentStep === 'ageGroups' && (
                    <AgeGroupsStep
                        formData={formData}
                        updateFormData={updateFormData}
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
                    Previous
                </button>

                <div className="text-sm text-gray-500">
                    Step {currentStepIndex + 1} of {STEPS.length}
                </div>

                {isLastStep ? (
                    <button
                        type="button"
                        onClick={handleSubmit}
                        disabled={!canProceed() || isSubmitting}
                        className="px-6 py-2 bg-indigo-600 text-white font-medium rounded-md hover:bg-indigo-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
                    >
                        {isSubmitting ? 'Saving...' : 'Complete Onboarding'}
                    </button>
                ) : (
                    <button
                        type="button"
                        onClick={handleNext}
                        disabled={!canProceed() || isSubmitting}
                        className="px-6 py-2 bg-indigo-600 text-white font-medium rounded-md hover:bg-indigo-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
                    >
                        Next
                    </button>
                )}
            </div>
        </div>
    );
}
