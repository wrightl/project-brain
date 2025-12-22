'use client';

interface ProgressIndicatorProps {
    completed: number;
    total: number;
}

export default function ProgressIndicator({ completed, total }: ProgressIndicatorProps) {
    const percentage = total > 0 ? Math.round((completed / total) * 100) : 0;

    return (
        <div className="bg-white rounded-lg shadow-sm p-6">
            <div className="flex items-center justify-between mb-2">
                <h2 className="text-lg font-semibold text-gray-900">Progress</h2>
                <span className="text-sm font-medium text-gray-600">
                    {completed} of {total} completed
                </span>
            </div>
            <div className="w-full bg-gray-200 rounded-full h-4">
                <div
                    className="bg-blue-600 h-4 rounded-full transition-all duration-300"
                    style={{ width: `${percentage}%` }}
                />
            </div>
            <p className="text-sm text-gray-500 mt-2">
                {percentage}% complete
            </p>
        </div>
    );
}

