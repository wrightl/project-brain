'use client';

interface AgeGroupsStepProps {
    formData: {
        ageGroups: string[];
    };
    updateFormData: (updates: Partial<AgeGroupsStepProps['formData']>) => void;
}

const AVAILABLE_AGE_GROUPS = [
    'Children (5-12)',
    'Teens (13-17)',
    'Young Adults (18-25)',
    'Adults (26-40)',
    'Middle-aged (41-60)',
    'Seniors (60+)',
];

export default function AgeGroupsStep({
    formData,
    updateFormData,
}: AgeGroupsStepProps) {
    const handleToggle = (ageGroup: string) => {
        const isSelected = formData.ageGroups.includes(ageGroup);
        if (isSelected) {
            updateFormData({
                ageGroups: formData.ageGroups.filter((ag) => ag !== ageGroup),
            });
        } else {
            updateFormData({
                ageGroups: [...formData.ageGroups, ageGroup],
            });
        }
    };

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-2xl font-bold text-gray-900">Age Groups</h2>
                <p className="mt-1 text-sm text-gray-600">
                    Select the age groups you work with. You can select multiple
                    age groups.
                </p>
            </div>

            <div className="flex flex-wrap gap-3">
                {AVAILABLE_AGE_GROUPS.map((ageGroup) => {
                    const isSelected = formData.ageGroups.includes(ageGroup);
                    return (
                        <button
                            key={ageGroup}
                            type="button"
                            onClick={() => handleToggle(ageGroup)}
                            className={`px-4 py-2 rounded-full text-sm font-medium transition-colors ${
                                isSelected
                                    ? 'bg-indigo-600 text-white hover:bg-indigo-700'
                                    : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                            }`}
                        >
                            {ageGroup}
                        </button>
                    );
                })}
            </div>

            {formData.ageGroups.length === 0 && (
                <div className="text-sm text-gray-500 italic">
                    No age groups selected yet. This step is optional.
                </div>
            )}
        </div>
    );
}
