'use client';

interface SpecialismsStepProps {
    formData: {
        specialisms: string[];
    };
    availableSpecialisms: string[];
    isLoading?: boolean;
    loadError?: string | null;
    updateFormData: (updates: Partial<SpecialismsStepProps['formData']>) => void;
}

export default function SpecialismsStep({
    formData,
    availableSpecialisms,
    isLoading = false,
    loadError = null,
    updateFormData,
}: SpecialismsStepProps) {
    const handleToggle = (specialism: string) => {
        const isSelected = formData.specialisms.includes(specialism);
        if (isSelected) {
            updateFormData({
                specialisms: formData.specialisms.filter((s) => s !== specialism),
            });
        } else {
            updateFormData({
                specialisms: [...formData.specialisms, specialism],
            });
        }
    };

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-2xl font-bold text-gray-900">
                    Specialisms
                </h2>
                <p className="mt-1 text-sm text-gray-600">
                    Select your coaching specialisms or areas of expertise. You
                    can select multiple specialisms.
                </p>
            </div>

            {isLoading && (
                <div className="text-sm text-gray-500">Loading specialisms...</div>
            )}

            {loadError && (
                <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded text-sm">
                    {loadError}
                </div>
            )}

            {!isLoading && !loadError && (
                <div className="flex flex-wrap gap-3">
                    {availableSpecialisms.map((specialism) => {
                        const isSelected =
                            formData.specialisms.includes(specialism);
                        return (
                            <button
                                key={specialism}
                                type="button"
                                onClick={() => handleToggle(specialism)}
                                className={`px-4 py-2 rounded-full text-sm font-medium transition-colors ${
                                    isSelected
                                        ? 'bg-indigo-600 text-white hover:bg-indigo-700'
                                        : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                                }`}
                            >
                                {specialism}
                            </button>
                        );
                    })}
                </div>
            )}

            {formData.specialisms.length === 0 && !isLoading && !loadError && (
                <div className="text-sm text-gray-500 italic">
                    No specialisms selected yet. This step is optional.
                </div>
            )}
        </div>
    );
}
