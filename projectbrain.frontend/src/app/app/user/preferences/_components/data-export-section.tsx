'use client';

import { useState } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import toast from 'react-hot-toast';

export default function DataExportSection() {
    const [downloading, setDownloading] = useState(false);

    const handleDownload = async () => {
        try {
            setDownloading(true);
            const response = await fetchWithAuth('/api/user/data-export');
            if (!response.ok) {
                throw new Error('Failed to download your data');
            }

            const blob = await response.blob();
            const disposition = response.headers.get('Content-Disposition');
            const filenameMatch = disposition?.match(/filename="(.+)"/);
            const filename =
                filenameMatch?.[1] ??
                `projectbrain-data-export-${new Date().toISOString().slice(0, 10)}.json`;

            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = filename;
            document.body.appendChild(link);
            link.click();
            link.remove();
            window.URL.revokeObjectURL(url);
            toast.success('Your data export is downloading');
        } catch (err) {
            toast.error(
                err instanceof Error ? err.message : 'Failed to download your data'
            );
        } finally {
            setDownloading(false);
        }
    };

    return (
        <div className="bg-white shadow rounded-lg p-6 border border-gray-300">
            <h2 className="text-xl font-semibold text-gray-900">Your data</h2>
            <p className="mt-1 text-sm text-gray-600">
                Download a copy of your profile, preferences, and learned memories
                as JSON.
            </p>
            <button
                type="button"
                onClick={handleDownload}
                disabled={downloading}
                className="mt-4 px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-900 bg-gray-100 hover:bg-gray-200 disabled:opacity-50"
            >
                {downloading ? 'Preparing download…' : 'Download my data'}
            </button>
        </div>
    );
}
