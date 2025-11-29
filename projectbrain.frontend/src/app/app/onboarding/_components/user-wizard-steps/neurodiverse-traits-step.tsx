'use client';

interface NeurodiverseTraitsStepProps {
    formData: {
        neurodiverseTraits: string[];
    };
    updateFormData: (updates: Partial<NeurodiverseTraitsStepProps['formData']>) => void;
}

// Predefined list of common neurodiverse traits
const AVAILABLE_TRAITS = [
    'ADHD',
    'Autism',
    'Dyslexia',
    'Dyscalculia',
    'Dyspraxia',
    'Dysgraphia',
    'Tourette Syndrome',
    'OCD',
    'Anxiety',
    'Depression',
    'Bipolar Disorder',
    'Sensory Processing Disorder',
    'Executive Function Challenges',
    'Social Communication Differences',
    'Giftedness',
    'Other',
];

export default function NeurodiverseTraitsStep({
    formData,
    updateFormData,
}: NeurodiverseTraitsStepProps) {
    const handleTraitToggle = (trait: string) => {
        const isSelected = formData.neurodiverseTraits.includes(trait);
        
        if (isSelected) {
            // Remove trait
            updateFormData({
                neurodiverseTraits: formData.neurodiverseTraits.filter((t) => t !== trait),
            });
        } else {
            // Add trait
            updateFormData({
                neurodiverseTraits: [...formData.neurodiverseTraits, trait],
            });
        }
    };

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-2xl font-bold text-gray-900">
                    Neurodiverse Traits
                </h2>
                <p className="mt-1 text-sm text-gray-600">
                    Select any neurodiverse traits or diagnoses that apply to you.
                    This information helps us provide better support and is
                    completely optional.
                </p>
            </div>

            <div>
                <h3 className="text-sm font-medium text-gray-700 mb-3">
                    Select traits that apply to you:
                </h3>
                <div className="flex flex-wrap gap-2">
                    {AVAILABLE_TRAITS.map((trait) => {
                        const isSelected = formData.neurodiverseTraits.includes(trait);
                        return (
                            <button
                                key={trait}
                                type="button"
                                onClick={() => handleTraitToggle(trait)}
                                className={`inline-flex items-center px-4 py-2 rounded-full text-sm font-medium transition-colors ${
                                    isSelected
                                        ? 'bg-indigo-600 text-white hover:bg-indigo-700'
                                        : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                                }`}
                            >
                                {trait}
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

            {formData.neurodiverseTraits.length > 0 && (
                <div className="space-y-2">
                    <h3 className="text-sm font-medium text-gray-700">
                        Selected Traits ({formData.neurodiverseTraits.length}):
                    </h3>
                    <div className="flex flex-wrap gap-2">
                        {formData.neurodiverseTraits.map((trait) => (
                            <span
                                key={trait}
                                className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-indigo-100 text-indigo-800"
                            >
                                {trait}
                            </span>
                        ))}
                    </div>
                </div>
            )}

            {formData.neurodiverseTraits.length === 0 && (
                <div className="text-sm text-gray-500 italic">
                    No traits selected yet. This step is optional.
                </div>
            )}
        </div>
    );
}

