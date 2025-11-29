'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { User } from '@/_lib/types';
import { apiClient } from '@/_lib/api-client';

interface ProfileFormProps {
    user: User;
}

export default function ProfileForm({ user: initialUser }: ProfileFormProps) {
    const router = useRouter();
    const [isEditing, setIsEditing] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);
    const [user, setUser] = useState<User>(initialUser);

    const [formData, setFormData] = useState({
        fullName: user.fullName || '',
        doB: user.doB || '',
        preferredPronoun: user.preferredPronoun || '',
        neurodiverseTraits: user.neurodiverseTraits || [],
        preferences: user.preferences || '',
        streetAddress: user.streetAddress || '',
        addressLine2: user.addressLine2 || '',
        city: user.city || '',
        stateProvince: user.stateProvince || '',
        postalCode: user.postalCode || '',
        country: user.country || '',
    });

    const [newTrait, setNewTrait] = useState('');

    useEffect(() => {
        setUser(initialUser);
        setFormData({
            fullName: initialUser.fullName || '',
            doB: initialUser.doB || '',
            preferredPronoun: initialUser.preferredPronoun || '',
            neurodiverseTraits: initialUser.neurodiverseTraits || [],
            preferences: initialUser.preferences || '',
            streetAddress: initialUser.streetAddress || '',
            addressLine2: initialUser.addressLine2 || '',
            city: initialUser.city || '',
            stateProvince: initialUser.stateProvince || '',
            postalCode: initialUser.postalCode || '',
            country: initialUser.country || '',
        });
    }, [initialUser]);

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

    const addTrait = () => {
        if (
            newTrait.trim() &&
            !formData.neurodiverseTraits.includes(newTrait.trim())
        ) {
            setFormData((prev) => ({
                ...prev,
                neurodiverseTraits: [
                    ...prev.neurodiverseTraits,
                    newTrait.trim(),
                ],
            }));
            setNewTrait('');
        }
    };

    const removeTrait = (trait: string) => {
        setFormData((prev) => ({
            ...prev,
            neurodiverseTraits: prev.neurodiverseTraits.filter(
                (t) => t !== trait
            ),
        }));
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setSuccess(null);
        setIsSubmitting(true);

        try {
            // Update user profile
            const updatedUser = await apiClient<User>(
                `/api/users/me/${user.id}`,
                {
                    method: 'PUT',
                    body: {
                        fullName: formData.fullName,
                        doB: formData.doB || undefined,
                        preferredPronoun:
                            formData.preferredPronoun || undefined,
                        neurodiverseTraits:
                            formData.neurodiverseTraits.length > 0
                                ? formData.neurodiverseTraits
                                : undefined,
                        preferences: formData.preferences || undefined,
                        streetAddress: formData.streetAddress || undefined,
                        addressLine2: formData.addressLine2 || undefined,
                        city: formData.city || undefined,
                        stateProvince: formData.stateProvince || undefined,
                        postalCode: formData.postalCode || undefined,
                        country: formData.country || undefined,
                    },
                }
            );
            setUser(updatedUser);

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
                                {user.email}
                            </p>
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-500">
                                Full Name
                            </label>
                            <p className="mt-1 text-sm text-gray-900">
                                {user.fullName}
                            </p>
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-500">
                                Date of Birth
                            </label>
                            <p className="mt-1 text-sm text-gray-900">
                                {user.doB || 'Not provided'}
                            </p>
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-500">
                                Preferred Pronoun
                            </label>
                            <p className="mt-1 text-sm text-gray-900">
                                {user.preferredPronoun || 'Not provided'}
                            </p>
                        </div>
                        <div className="sm:col-span-2">
                            <label className="block text-sm font-medium text-gray-500">
                                Neurodiverse Traits
                            </label>
                            <div className="mt-1 flex flex-wrap gap-2">
                                {user.neurodiverseTraits &&
                                user.neurodiverseTraits.length > 0 ? (
                                    user.neurodiverseTraits.map(
                                        (trait, index) => (
                                            <span
                                                key={index}
                                                className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-indigo-100 text-indigo-800"
                                            >
                                                {trait}
                                            </span>
                                        )
                                    )
                                ) : (
                                    <p className="text-sm text-gray-500">
                                        None specified
                                    </p>
                                )}
                            </div>
                        </div>
                        <div className="sm:col-span-2">
                            <label className="block text-sm font-medium text-gray-500">
                                Preferences
                            </label>
                            <p className="mt-1 text-sm text-gray-900">
                                {user.preferences || 'Not provided'}
                            </p>
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
                                    {user.streetAddress || 'Not provided'}
                                </p>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-500">
                                    Address Line 2
                                </label>
                                <p className="mt-1 text-sm text-gray-900">
                                    {user.addressLine2 || 'Not provided'}
                                </p>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-500">
                                    City
                                </label>
                                <p className="mt-1 text-sm text-gray-900">
                                    {user.city || 'Not provided'}
                                </p>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-500">
                                    State/Province
                                </label>
                                <p className="mt-1 text-sm text-gray-900">
                                    {user.stateProvince || 'Not provided'}
                                </p>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-500">
                                    Postal Code
                                </label>
                                <p className="mt-1 text-sm text-gray-900">
                                    {user.postalCode || 'Not provided'}
                                </p>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-gray-500">
                                    Country
                                </label>
                                <p className="mt-1 text-sm text-gray-900">
                                    {user.country || 'Not provided'}
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
                            value={user.email}
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

                    <div>
                        <label
                            htmlFor="doB"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Date of Birth
                        </label>
                        <input
                            type="date"
                            id="doB"
                            name="doB"
                            value={formData.doB}
                            onChange={handleChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                    </div>

                    <div>
                        <label
                            htmlFor="preferredPronoun"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Preferred Pronoun
                        </label>
                        <select
                            id="preferredPronoun"
                            name="preferredPronoun"
                            value={formData.preferredPronoun}
                            onChange={handleChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        >
                            <option value="">Select pronouns</option>
                            <option value="he/him">He/Him</option>
                            <option value="she/her">She/Her</option>
                            <option value="they/them">They/Them</option>
                            <option value="other">
                                Other/Prefer to self-describe
                            </option>
                        </select>
                    </div>

                    <div className="sm:col-span-2">
                        <label className="block text-sm font-medium text-gray-700 mb-2">
                            Neurodiverse Traits
                        </label>
                        <div className="flex gap-2 mb-2">
                            <input
                                type="text"
                                value={newTrait}
                                onChange={(e) => setNewTrait(e.target.value)}
                                onKeyPress={(e) => {
                                    if (e.key === 'Enter') {
                                        e.preventDefault();
                                        addTrait();
                                    }
                                }}
                                placeholder="Add a trait"
                                className="flex-1 rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            />
                            <button
                                type="button"
                                onClick={addTrait}
                                className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-md hover:bg-indigo-700"
                            >
                                Add
                            </button>
                        </div>
                        <div className="flex flex-wrap gap-2">
                            {formData.neurodiverseTraits.map((trait, index) => (
                                <span
                                    key={index}
                                    className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-indigo-100 text-indigo-800"
                                >
                                    {trait}
                                    <button
                                        type="button"
                                        onClick={() => removeTrait(trait)}
                                        className="ml-2 text-indigo-600 hover:text-indigo-800"
                                    >
                                        ×
                                    </button>
                                </span>
                            ))}
                        </div>
                    </div>

                    <div className="sm:col-span-2">
                        <label
                            htmlFor="preferences"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Preferences
                        </label>
                        <textarea
                            id="preferences"
                            name="preferences"
                            rows={3}
                            value={formData.preferences}
                            onChange={handleChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
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
