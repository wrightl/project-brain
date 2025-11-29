'use client';

import { useState } from 'react';

interface QualificationsStepProps {
    formData: {
        qualifications: string[];
    };
    updateFormData: (updates: Partial<QualificationsStepProps['formData']>) => void;
}

export default function QualificationsStep({
    formData,
    updateFormData,
}: QualificationsStepProps) {
    const [newQualification, setNewQualification] = useState('');

    const handleAdd = () => {
        if (newQualification.trim() !== '') {
            updateFormData({
                qualifications: [...formData.qualifications, newQualification.trim()],
            });
            setNewQualification('');
        }
    };

    const handleRemove = (index: number) => {
        updateFormData({
            qualifications: formData.qualifications.filter((_, i) => i !== index),
        });
    };

    const handleKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            handleAdd();
        }
    };

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-2xl font-bold text-gray-900">
                    Qualifications
                </h2>
                <p className="mt-1 text-sm text-gray-600">
                    Add your coaching qualifications. You can add multiple
                    qualifications.
                </p>
            </div>

            <div className="flex gap-2">
                <input
                    type="text"
                    value={newQualification}
                    onChange={(e) => setNewQualification(e.target.value)}
                    onKeyPress={handleKeyPress}
                    placeholder="Enter a qualification (e.g., Level 3 Coaching Certificate)"
                    className="flex-1 rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                />
                <button
                    type="button"
                    onClick={handleAdd}
                    className="px-4 py-2 bg-indigo-600 text-white font-medium rounded-md hover:bg-indigo-700 transition-colors"
                >
                    Add
                </button>
            </div>

            {formData.qualifications.length > 0 && (
                <div className="space-y-2">
                    <h3 className="text-sm font-medium text-gray-700">
                        Your Qualifications:
                    </h3>
                    <ul className="space-y-2">
                        {formData.qualifications.map((qualification, index) => (
                            <li
                                key={index}
                                className="flex items-center justify-between bg-gray-50 rounded-md p-3"
                            >
                                <span className="text-sm text-gray-900">
                                    {qualification}
                                </span>
                                <button
                                    type="button"
                                    onClick={() => handleRemove(index)}
                                    className="ml-4 text-red-600 hover:text-red-800 transition-colors"
                                >
                                    <svg
                                        className="w-5 h-5"
                                        fill="none"
                                        stroke="currentColor"
                                        viewBox="0 0 24 24"
                                    >
                                        <path
                                            strokeLinecap="round"
                                            strokeLinejoin="round"
                                            strokeWidth={2}
                                            d="M6 18L18 6M6 6l12 12"
                                        />
                                    </svg>
                                </button>
                            </li>
                        ))}
                    </ul>
                </div>
            )}

            {formData.qualifications.length === 0 && (
                <div className="text-sm text-gray-500 italic">
                    No qualifications added yet. This step is optional.
                </div>
            )}
        </div>
    );
}

