'use client';

interface BasicInfoStepProps {
    formData: {
        email: string;
        fullName: string;
        doB: string;
        preferredPronoun: string;
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

                <div>
                    <label
                        htmlFor="doB"
                        className="block text-sm font-medium text-gray-700"
                    >
                        Date of Birth *
                    </label>
                    <input
                        type="date"
                        id="doB"
                        name="doB"
                        required
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
                        Preferred Pronouns *
                    </label>
                    <select
                        id="preferredPronoun"
                        name="preferredPronoun"
                        required
                        value={formData.preferredPronoun}
                        onChange={handleChange}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    >
                        <option value="">Select pronouns</option>
                        <option value="he/him">He/Him</option>
                        <option value="she/her">She/Her</option>
                        <option value="they/them">They/Them</option>
                        <option value="other">Other/Prefer to self-describe</option>
                    </select>
                </div>
            </div>
        </div>
    );
}

