'use client';

interface BasicInfoStepProps {
    formData: {
        email: string;
        fullName: string;
        streetAddress?: string;
        addressLine2?: string;
        city?: string;
        stateProvince?: string;
        postalCode?: string;
        country?: string;
    };
    updateFormData: (updates: Partial<BasicInfoStepProps['formData']>) => void;
}

export default function BasicInfoStep({
    formData,
    updateFormData,
}: BasicInfoStepProps) {
    const handleChange = (
        e: React.ChangeEvent<
            HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
        >
    ) => {
        updateFormData({ [e.target.name]: e.target.value });
    };

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-2xl font-bold text-gray-900">
                    Basic Information
                </h2>
                <p className="mt-1 text-sm text-gray-600">
                    Please provide your basic information to get started
                </p>
            </div>

            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
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
                        htmlFor="email"
                        className="block text-sm font-medium text-gray-700"
                    >
                        Email *
                    </label>
                    <input
                        type="email"
                        id="email"
                        name="email"
                        required
                        value={formData.email}
                        onChange={handleChange}
                        disabled
                        className="mt-1 block w-full rounded-md border-gray-300 bg-gray-50 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm cursor-not-allowed"
                    />
                </div>

            </div>

            <div className="border-t border-gray-200 pt-6">
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
                            value={formData.streetAddress || ''}
                            onChange={handleChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                    </div>

                    <div>
                        <label
                            htmlFor="addressLine2"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Address Line 2 (Optional)
                        </label>
                        <input
                            type="text"
                            id="addressLine2"
                            name="addressLine2"
                            value={formData.addressLine2 || ''}
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
                                value={formData.city || ''}
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
                                value={formData.stateProvince || ''}
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
                                value={formData.postalCode || ''}
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
                                value={formData.country || ''}
                                onChange={handleChange}
                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

