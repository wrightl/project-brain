'use client';

import { useState } from 'react';

interface SpecialismsStepProps {
    formData: {
        specialisms: string[];
    };
    updateFormData: (updates: Partial<SpecialismsStepProps['formData']>) => void;
}

export default function SpecialismsStep({
    formData,
    updateFormData,
}: SpecialismsStepProps) {
    const [newSpecialism, setNewSpecialism] = useState('');

    const handleAdd = () => {
        if (newSpecialism.trim() !== '') {
            updateFormData({
                specialisms: [...formData.specialisms, newSpecialism.trim()],
            });
            setNewSpecialism('');
        }
    };

    const handleRemove = (index: number) => {
        updateFormData({
            specialisms: formData.specialisms.filter((_, i) => i !== index),
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
                    Specialisms
                </h2>
                <p className="mt-1 text-sm text-gray-600">
                    Add your coaching specialisms or areas of expertise. You can
                    add multiple specialisms.
                </p>
            </div>

            <div className="flex gap-2">
                <input
                    type="text"
                    value={newSpecialism}
                    onChange={(e) => setNewSpecialism(e.target.value)}
                    onKeyPress={handleKeyPress}
                    placeholder="Enter a specialism (e.g., Youth Development, Performance Coaching)"
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

            {formData.specialisms.length > 0 && (
                <div className="space-y-2">
                    <h3 className="text-sm font-medium text-gray-700">
                        Your Specialisms:
                    </h3>
                    <ul className="space-y-2">
                        {formData.specialisms.map((specialism, index) => (
                            <li
                                key={index}
                                className="flex items-center justify-between bg-gray-50 rounded-md p-3"
                            >
                                <span className="text-sm text-gray-900">
                                    {specialism}
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

            {formData.specialisms.length === 0 && (
                <div className="text-sm text-gray-500 italic">
                    No specialisms added yet. This step is optional.
                </div>
            )}
        </div>
    );
}

