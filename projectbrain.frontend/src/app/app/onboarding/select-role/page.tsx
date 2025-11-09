'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';

export default function SelectRolePage() {
    const router = useRouter();
    const [selectedRole, setSelectedRole] = useState('');

    const handleRoleSelect = (role: string) => {
        setSelectedRole(role);
        router.push(`/app/onboarding/${role.toLowerCase()}`);
    };

    return (
        <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
            <div className="max-w-md w-full space-y-8">
                <div>
                    <h2 className="text-center text-3xl font-extrabold text-gray-900">
                        Select Your Role
                    </h2>
                    <p className="mt-2 text-center text-sm text-gray-600">
                        Are you signing up as a normal user or a coach?
                    </p>
                </div>
                <div className="bg-white shadow rounded-lg p-8 space-y-4">
                    <button
                        onClick={() => handleRoleSelect('User')}
                        className="w-full flex justify-center py-3 px-4 border border-transparent rounded-md shadow-sm text-lg font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
                    >
                        Sign up as a User
                    </button>
                    <button
                        onClick={() => handleRoleSelect('Coach')}
                        className="w-full flex justify-center py-3 px-4 border border-transparent rounded-md shadow-sm text-lg font-medium text-indigo-700 bg-indigo-100 hover:bg-indigo-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
                    >
                        Sign up as a Coach
                    </button>
                </div>
            </div>
        </div>
    );
}
