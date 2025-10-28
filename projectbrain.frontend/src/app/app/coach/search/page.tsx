import { RoleGuard } from '@/_components/auth/role-guard';
import { MagnifyingGlassIcon, MapPinIcon } from '@heroicons/react/24/outline';

export default async function CoachSearchPage() {
    // TODO: Implement user search API endpoint

    return (
        <RoleGuard allowedRoles={['coach']}>
            <div className="space-y-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">
                        Find Users
                    </h1>
                    <p className="mt-1 text-sm text-gray-600">
                        Search for users in your geographical region
                    </p>
                </div>

                {/* Search Form */}
                <div className="bg-white shadow rounded-lg p-6">
                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                        <div>
                            <label
                                htmlFor="region"
                                className="block text-sm font-medium text-gray-700"
                            >
                                Region / Location
                            </label>
                            <div className="mt-1 relative">
                                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                    <MapPinIcon className="h-5 w-5 text-gray-400" />
                                </div>
                                <input
                                    type="text"
                                    id="region"
                                    placeholder="Enter city, state, or zip code"
                                    className="block w-full pl-10 rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                />
                            </div>
                        </div>
                        <div>
                            <label
                                htmlFor="radius"
                                className="block text-sm font-medium text-gray-700"
                            >
                                Search Radius
                            </label>
                            <select
                                id="radius"
                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            >
                                <option value="10">Within 10 miles</option>
                                <option value="25">Within 25 miles</option>
                                <option value="50">Within 50 miles</option>
                                <option value="100">Within 100 miles</option>
                                <option value="any">Any distance</option>
                            </select>
                        </div>
                    </div>

                    <div className="mt-4">
                        <button
                            type="button"
                            className="inline-flex items-center px-4 py-2 bg-indigo-600 text-white font-medium rounded-md hover:bg-indigo-700 transition-colors"
                        >
                            <MagnifyingGlassIcon className="h-5 w-5 mr-2" />
                            Search Users
                        </button>
                    </div>
                </div>

                {/* Results */}
                <div className="bg-white shadow rounded-lg p-6">
                    <h2 className="text-lg font-medium text-gray-900 mb-4">
                        Search Results
                    </h2>
                    <div className="text-center py-12 text-gray-500">
                        <MagnifyingGlassIcon className="mx-auto h-12 w-12 text-gray-400" />
                        <p className="mt-2">No results to display</p>
                        <p className="text-sm">
                            Enter search criteria and click &quot;Search
                            Users&quot; to find users in your area
                        </p>
                    </div>
                </div>

                {/* Info Box */}
                <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
                    <h3 className="text-sm font-medium text-blue-900 mb-2">
                        About User Search
                    </h3>
                    <ul className="text-sm text-blue-700 space-y-1 list-disc list-inside">
                        <li>
                            Search results are filtered based on geographical
                            proximity
                        </li>
                        <li>
                            User contact information is protected and only
                            shared with mutual consent
                        </li>
                        <li>Users must opt-in to be discoverable by coaches</li>
                        <li>
                            API endpoint for user search needs to be implemented
                        </li>
                    </ul>
                </div>
            </div>
        </RoleGuard>
    );
}
