import { getSession } from '@auth0/nextjs-auth0';
import Link from 'next/link';

export default async function TestAuthPage() {
  const session = await getSession();

  return (
    <div className="min-h-screen bg-gray-50 py-12 px-4">
      <div className="max-w-4xl mx-auto">
        <div className="bg-white shadow rounded-lg p-6">
          <h1 className="text-2xl font-bold mb-4">Auth Test Page</h1>

          {!session ? (
            <div>
              <p className="text-gray-600 mb-4">Not logged in</p>
              <a
                href="/auth/login"
                className="inline-block px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700"
              >
                Log In
              </a>
            </div>
          ) : (
            <div className="space-y-4">
              <div>
                <h2 className="text-lg font-semibold mb-2">Session Data</h2>
                <pre className="bg-gray-100 p-4 rounded overflow-x-auto text-xs">
                  {JSON.stringify(session, null, 2)}
                </pre>
              </div>

              <div className="flex space-x-4">
                <Link
                  href="/dashboard"
                  className="inline-block px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700"
                >
                  Go to Dashboard
                </Link>
                <a
                  href="/auth/logout"
                  className="inline-block px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700"
                >
                  Log Out
                </a>
              </div>
            </div>
          )}
        </div>

        <div className="mt-8 bg-blue-50 border border-blue-200 rounded-lg p-6">
          <h2 className="text-lg font-semibold mb-2 text-blue-900">
            Troubleshooting
          </h2>
          <ul className="text-sm text-blue-700 space-y-2">
            <li>✓ If you see session data, authentication is working</li>
            <li>✓ Check if <code>accessToken</code> is present (needed for API calls)</li>
            <li>✓ Check if custom role claim is present: <code>https://projectbrain.app/role</code></li>
            <li>✓ If no session, try logging in via the button above</li>
          </ul>
        </div>
      </div>
    </div>
  );
}
