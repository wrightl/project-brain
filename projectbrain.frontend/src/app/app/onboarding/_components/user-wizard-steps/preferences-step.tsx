'use client';

interface PreferencesStepProps {
    formData: {
        preferences: string;
    };
    updateFormData: (updates: Partial<PreferencesStepProps['formData']>) => void;
}

export default function PreferencesStep({
    formData,
    updateFormData,
}: PreferencesStepProps) {
    const handleChange = (
        e: React.ChangeEvent<HTMLTextAreaElement>
    ) => {
        updateFormData({ [e.target.name]: e.target.value });
    };

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-2xl font-bold text-gray-900">
                    Preferences
                </h2>
                <p className="mt-1 text-sm text-gray-600">
                    Share any preferences or information that would help us
                    provide you with better support. This is completely optional.
                </p>
            </div>

            <div>
                <label
                    htmlFor="preferences"
                    className="block text-sm font-medium text-gray-700"
                >
                    Your Preferences
                </label>
                <textarea
                    id="preferences"
                    name="preferences"
                    rows={6}
                    value={formData.preferences}
                    onChange={handleChange}
                    placeholder="Enter any preferences, accommodations, or information you'd like us to know..."
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                />
                <p className="mt-2 text-xs text-gray-500">
                    This information will be kept confidential and used to
                    personalize your experience.
                </p>
            </div>
        </div>
    );
}

