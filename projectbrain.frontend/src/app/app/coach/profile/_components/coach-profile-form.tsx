'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Coach } from '@/_lib/types';
import { apiClient } from '@/_lib/api-client';

interface CoachProfileFormProps {
    coach: Coach;
}

export default function CoachProfileForm({ coach: initialCoach }: CoachProfileFormProps) {
    const router = useRouter();
    const [isEditing, setIsEditing] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);
    const [coach, setCoach] = useState<Coach>(initialCoach);

    const [formData, setFormData] = useState({
        fullName: coach.fullName || '',
        streetAddress: coach.streetAddress || '',
        addressLine2: coach.addressLine2 || '',
        city: coach.city || '',
        stateProvince: coach.stateProvince || '',
        postalCode: coach.postalCode || '',
        country: coach.country || '',
        qualifications: coach.qualifications || [],
        specialisms: coach.specialisms || [],
        ageGroups: coach.ageGroups || [],
    });

    const [newQualification, setNewQualification] = useState('');
    const [newSpecialism, setNewSpecialism] = useState('');
    const [newAgeGroup, setNewAgeGroup] = useState('');

    useEffect(() => {
        setCoach(initialCoach);
        setFormData({
            fullName: initialCoach.fullName || '',
            streetAddress: initialCoach.streetAddress || '',
            addressLine2: initialCoach.addressLine2 || '',
            city: initialCoach.city || '',
            stateProvince: initialCoach.stateProvince || '',
            postalCode: initialCoach.postalCode || '',
            country: initialCoach.country || '',
            qualifications: initialCoach.qualifications || [],
            specialisms: initialCoach.specialisms || [],
            ageGroups: initialCoach.ageGroups || [],
        });
    }, [initialCoach]);

    const handleChange = (
        e: React.ChangeEvent<
            HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
        >
    ) => {
        const { name, value } = e.target;
        setFormData((prev) => ({
            ...prev,
            [name]: value,
        }));
    };

    const addQualification = () => {
        if (
            newQualification.trim() &&
            !formData.qualifications.includes(newQualification.trim())
        ) {
            setFormData((prev) => ({
                ...prev,
                qualifications: [
                    ...prev.qualifications,
                    newQualification.trim(),
                ],
            }));
            setNewQualification('');
        }
    };

    const removeQualification = (qual: string) => {
        setFormData((prev) => ({
            ...prev,
            qualifications: prev.qualifications.filter((q) => q !== qual),
        }));
    };

    const addSpecialism = () => {
        if (
            newSpecialism.trim() &&
            !formData.specialisms.includes(newSpecialism.trim())
        ) {
            setFormData((prev) => ({
                ...prev,
                specialisms: [...prev.specialisms, newSpecialism.trim()],
            }));
            setNewSpecialism('');
        }
    };

    const removeSpecialism = (spec: string) => {
        setFormData((prev) => ({
            ...prev,
            specialisms: prev.specialisms.filter((s) => s !== spec),
        }));
    };

    const addAgeGroup = () => {
        if (
            newAgeGroup.trim() &&
            !formData.ageGroups.includes(newAgeGroup.trim())
        ) {
            setFormData((prev) => ({
                ...prev,
                ageGroups: [...prev.ageGroups, newAgeGroup.trim()],
            }));
            setNewAgeGroup('');
        }
    };

    const removeAgeGroup = (ageGroup: string) => {
        setFormData((prev) => ({
            ...prev,
            ageGroups: prev.ageGroups.filter((ag) => ag !== ageGroup),
        }));
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setSuccess(null);
        setIsSubmitting(true);

        try {
            const updatedCoach = await apiClient<Coach>(
                `/api/coaches/me/${coach.id}`,
                {
                    method: 'PUT',
                    body: {
                        fullName: formData.fullName,
                        streetAddress: formData.streetAddress || undefined,
                        addressLine2: formData.addressLine2 || undefined,
                        city: formData.city || undefined,
                        stateProvince: formData.stateProvince || undefined,
                        postalCode: formData.postalCode || undefined,
                        country: formData.country || undefined,
                        qualifications:
                            formData.qualifications.length > 0
                                ? formData.qualifications
                                : undefined,
                        specialisms:
                            formData.specialisms.length > 0
                                ? formData.specialisms
                                : undefined,
                        ageGroups:
                            formData.ageGroups.length > 0
                                ? formData.ageGroups
                                : undefined,
                    },
                }
            );
            setCoach(updatedCoach);
            setSuccess('Profile updated successfully!');
            setIsEditing(false);
            router.refresh();
        } catch (err) {
            setError(
                err instanceof Error ? err.message : 'Failed to update profile'
            );
        } finally {
            setIsSubmitting(false);
        }
    };

    if (!isEditing) {
        return (
            <div className="bg-white shadow rounded-lg">
                <div className="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
                    <h2 className="text-xl font-semibold text-gray-900">
                        Profile Information
                    </h2>
                    <button
                        onClick={() => setIsEditing(true)}
                        className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-md hover:bg-indigo-700"
                    >
                        Edit Profile
                    </button>
                </div>
                <div className="px-6 py-4 space-y-6">
                    <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                        <div>
                            <label className="block text-sm font-medium text-gray-500">
                                Email
                            </label>
                            <p className="mt-1 text-sm text-gray-900">
                                {coach.email}
                            </p>
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-500">
                                Full Name
                            </label>
                            <p className="mt-1 text-sm text-gray-900">
                                {coach.fullName}
                            </p>
                        </div>
                        <div className="sm:col-span-2">
                            <label className="block text-sm font-medium text-gray-500">
                                Qualifications
                            </label>
                            <div className="mt-1 flex flex-wrap gap-2">
                                {coach.qualifications &&
                                coach.qualifications.length > 0 ? (
                                    coach.qualifications.map((qual, index) => (
                                        <span
                                            key={index}
                                            className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-purple-100 text-purple-800"
                                        >
                                            {qual}
                                        </span>
                                    ))
                                ) : (
                                    <p className="text-sm text-gray-500">
                                        None specified
                                    </p>
                                )}
                            </div>
                        </div>
                        <div className="sm:col-span-2">
                            <label className="block text-sm font-medium text-gray-500">
                                Specialisms
                            </label>
                            <div className="mt-1 flex flex-wrap gap-2">
                                {coach.specialisms &&
                                coach.specialisms.length > 0 ? (
                                    coach.specialisms.map((spec, index) => (
                                        <span
                                            key={index}
                                            className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800"
                                        >
                                            {spec}
                                        </span>
                                    ))
                                ) : (
                                    <p className="text-sm text-gray-500">
                                        None specified
                                    </p>
                                )}
                            </div>
                        </div>
                        <div className="sm:col-span-2">
                            <label className="block text-sm font-medium text-gray-500">
                                Age Groups
                            </label>
                            <div className="mt-1 flex flex-wrap gap-2">
                                {coach.ageGroups &&
                                coach.ageGroups.length > 0 ? (
                                    coach.ageGroups.map((ag, index) => (
                                        <span
                                            key={index}
                                            className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800"
                                        >
                                            {ag}
                                        </span>
                                    ))
                                ) : (
                                    <p className="text-sm text-gray-500">
                                        None specified
                                    </p>
                                )}
                            </div>
                        </div>
                    </div>

                    <div className="border-t border-gray-200 pt-6">
                        <h3 className="text-lg font-medium text-gray-900 mb-4">
                            Address Information
                        </h3>
                        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                            <div>
                                <label className="block text-sm font-medium text-gray-500">
                                    Street Address
                                </label>
                                <p className="mt-1 text-sm text-gray-900">
                                    {coach.streetAddress || 'Not provided'}
                                </p>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-500">
                                    Address Line 2
                                </label>
                                <p className="mt-1 text-sm text-gray-900">
                                    {coach.addressLine2 || 'Not provided'}
                                </p>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-500">
                                    City
                                </label>
                                <p className="mt-1 text-sm text-gray-900">
                                    {coach.city || 'Not provided'}
                                </p>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-500">
                                    State/Province
                                </label>
                                <p className="mt-1 text-sm text-gray-900">
                                    {coach.stateProvince || 'Not provided'}
                                </p>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-500">
                                    Postal Code
                                </label>
                                <p className="mt-1 text-sm text-gray-900">
                                    {coach.postalCode || 'Not provided'}
                                </p>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-500">
                                    Country
                                </label>
                                <p className="mt-1 text-sm text-gray-900">
                                    {coach.country || 'Not provided'}
                                </p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <form onSubmit={handleSubmit} className="bg-white shadow rounded-lg">
            <div className="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
                <h2 className="text-xl font-semibold text-gray-900">
                    Edit Profile
                </h2>
                <button
                    type="button"
                    onClick={() => {
                        setIsEditing(false);
                        setError(null);
                        setSuccess(null);
                    }}
                    className="text-sm text-gray-600 hover:text-gray-900"
                >
                    Cancel
                </button>
            </div>

            <div className="px-6 py-4 space-y-6">
                {error && (
                    <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
                        {error}
                    </div>
                )}
                {success && (
                    <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded">
                        {success}
                    </div>
                )}

                <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                    <div className="sm:col-span-2">
                        <label
                            htmlFor="email"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Email
                        </label>
                        <input
                            type="email"
                            id="email"
                            value={coach.email}
                            disabled
                            className="mt-1 block w-full rounded-md border-gray-300 bg-gray-50 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm cursor-not-allowed"
                        />
                    </div>

                    <div>
                        <label
                            htmlFor="fullName"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Full Name *
                        </label>
                        <input
                            type="text"
                            id="fullName"
                            name="fullName"
                            required
                            value={formData.fullName}
                            onChange={handleChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                    </div>

                    <div className="sm:col-span-2">
                        <label className="block text-sm font-medium text-gray-700 mb-2">
                            Qualifications
                        </label>
                        <div className="flex gap-2 mb-2">
                            <input
                                type="text"
                                value={newQualification}
                                onChange={(e) =>
                                    setNewQualification(e.target.value)
                                }
                                onKeyPress={(e) => {
                                    if (e.key === 'Enter') {
                                        e.preventDefault();
                                        addQualification();
                                    }
                                }}
                                placeholder="Add a qualification"
                                className="flex-1 rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            />
                            <button
                                type="button"
                                onClick={addQualification}
                                className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-md hover:bg-indigo-700"
                            >
                                Add
                            </button>
                        </div>
                        <div className="flex flex-wrap gap-2">
                            {formData.qualifications.map((qual, index) => (
                                <span
                                    key={index}
                                    className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-purple-100 text-purple-800"
                                >
                                    {qual}
                                    <button
                                        type="button"
                                        onClick={() =>
                                            removeQualification(qual)
                                        }
                                        className="ml-2 text-purple-600 hover:text-purple-800"
                                    >
                                        ×
                                    </button>
                                </span>
                            ))}
                        </div>
                    </div>

                    <div className="sm:col-span-2">
                        <label className="block text-sm font-medium text-gray-700 mb-2">
                            Specialisms
                        </label>
                        <div className="flex gap-2 mb-2">
                            <input
                                type="text"
                                value={newSpecialism}
                                onChange={(e) =>
                                    setNewSpecialism(e.target.value)
                                }
                                onKeyPress={(e) => {
                                    if (e.key === 'Enter') {
                                        e.preventDefault();
                                        addSpecialism();
                                    }
                                }}
                                placeholder="Add a specialism"
                                className="flex-1 rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            />
                            <button
                                type="button"
                                onClick={addSpecialism}
                                className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-md hover:bg-indigo-700"
                            >
                                Add
                            </button>
                        </div>
                        <div className="flex flex-wrap gap-2">
                            {formData.specialisms.map((spec, index) => (
                                <span
                                    key={index}
                                    className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800"
                                >
                                    {spec}
                                    <button
                                        type="button"
                                        onClick={() => removeSpecialism(spec)}
                                        className="ml-2 text-green-600 hover:text-green-800"
                                    >
                                        ×
                                    </button>
                                </span>
                            ))}
                        </div>
                    </div>

                    <div className="sm:col-span-2">
                        <label className="block text-sm font-medium text-gray-700 mb-2">
                            Age Groups
                        </label>
                        <div className="flex gap-2 mb-2">
                            <input
                                type="text"
                                value={newAgeGroup}
                                onChange={(e) => setNewAgeGroup(e.target.value)}
                                onKeyPress={(e) => {
                                    if (e.key === 'Enter') {
                                        e.preventDefault();
                                        addAgeGroup();
                                    }
                                }}
                                placeholder="Add an age group"
                                className="flex-1 rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            />
                            <button
                                type="button"
                                onClick={addAgeGroup}
                                className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-md hover:bg-indigo-700"
                            >
                                Add
                            </button>
                        </div>
                        <div className="flex flex-wrap gap-2">
                            {formData.ageGroups.map((ag, index) => (
                                <span
                                    key={index}
                                    className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800"
                                >
                                    {ag}
                                    <button
                                        type="button"
                                        onClick={() => removeAgeGroup(ag)}
                                        className="ml-2 text-blue-600 hover:text-blue-800"
                                    >
                                        ×
                                    </button>
                                </span>
                            ))}
                        </div>
                    </div>

                    <div className="sm:col-span-2 border-t border-gray-200 pt-6">
                        <h3 className="text-lg font-medium text-gray-900 mb-4">
                            Address Information
                        </h3>
                        <div className="space-y-4">
                            <div>
                                <label
                                    htmlFor="streetAddress"
                                    className="block text-sm font-medium text-gray-700"
                                >
                                    Street Address
                                </label>
                                <input
                                    type="text"
                                    id="streetAddress"
                                    name="streetAddress"
                                    value={formData.streetAddress}
                                    onChange={handleChange}
                                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                />
                            </div>

                            <div>
                                <label
                                    htmlFor="addressLine2"
                                    className="block text-sm font-medium text-gray-700"
                                >
                                    Address Line 2
                                </label>
                                <input
                                    type="text"
                                    id="addressLine2"
                                    name="addressLine2"
                                    value={formData.addressLine2}
                                    onChange={handleChange}
                                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                />
                            </div>

                            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                                <div>
                                    <label
                                        htmlFor="city"
                                        className="block text-sm font-medium text-gray-700"
                                    >
                                        City
                                    </label>
                                    <input
                                        type="text"
                                        id="city"
                                        name="city"
                                        value={formData.city}
                                        onChange={handleChange}
                                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                    />
                                </div>

                                <div>
                                    <label
                                        htmlFor="stateProvince"
                                        className="block text-sm font-medium text-gray-700"
                                    >
                                        State/Province/Region
                                    </label>
                                    <input
                                        type="text"
                                        id="stateProvince"
                                        name="stateProvince"
                                        value={formData.stateProvince}
                                        onChange={handleChange}
                                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                    />
                                </div>
                            </div>

                            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                                <div>
                                    <label
                                        htmlFor="postalCode"
                                        className="block text-sm font-medium text-gray-700"
                                    >
                                        Postal/Zip Code
                                    </label>
                                    <input
                                        type="text"
                                        id="postalCode"
                                        name="postalCode"
                                        value={formData.postalCode}
                                        onChange={handleChange}
                                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                    />
                                </div>

                                <div>
                                    <label
                                        htmlFor="country"
                                        className="block text-sm font-medium text-gray-700"
                                    >
                                        Country
                                    </label>
                                    <input
                                        type="text"
                                        id="country"
                                        name="country"
                                        value={formData.country}
                                        onChange={handleChange}
                                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                    />
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div className="flex justify-end space-x-3 pt-4 border-t border-gray-200">
                    <button
                        type="button"
                        onClick={() => {
                            setIsEditing(false);
                            setError(null);
                            setSuccess(null);
                        }}
                        className="px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50"
                    >
                        Cancel
                    </button>
                    <button
                        type="submit"
                        disabled={isSubmitting}
                        className="px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 disabled:bg-gray-400"
                    >
                        {isSubmitting ? 'Saving...' : 'Save Changes'}
                    </button>
                </div>
            </div>
        </form>
    );
}

