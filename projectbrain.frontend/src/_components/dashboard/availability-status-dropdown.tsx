'use client';

import { useState, useEffect, useRef } from 'react';
import { ChevronDownIcon } from '@heroicons/react/24/outline';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';

type AvailabilityStatus = 'Available' | 'Busy' | 'Away' | 'Offline';

const statusColors = {
    Available: 'bg-green-100 text-green-800 border-green-300',
    Busy: 'bg-red-100 text-red-800 border-red-300',
    Away: 'bg-yellow-100 text-yellow-800 border-yellow-300',
    Offline: 'bg-gray-100 text-gray-800 border-gray-300',
};

const statusLabels = {
    Available: 'Available',
    Busy: 'Busy',
    Away: 'Away',
    Offline: 'Offline',
};

export default function AvailabilityStatusDropdown() {
    const [status, setStatus] = useState<AvailabilityStatus>('Available');
    const [isOpen, setIsOpen] = useState(false);
    const dropdownRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        // Load current status
        fetchWithAuth('/api/coach/availability/status')
            .then((res) => {
                if (!res.ok) {
                    throw new Error('Failed to load availability status');
                }
                return res.json();
            })
            .then((data: { status: AvailabilityStatus }) =>
                setStatus(data.status)
            )
            .catch((err) => {
                console.error('Failed to load availability status:', err);
            });

        // Close dropdown when clicking outside
        const handleClickOutside = (event: MouseEvent) => {
            if (
                dropdownRef.current &&
                !dropdownRef.current.contains(event.target as Node)
            ) {
                setIsOpen(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, []);

    const handleStatusChange = async (newStatus: AvailabilityStatus) => {
        try {
            const response = await fetch('/api/coach/availability/status', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ status: newStatus }),
            });

            if (!response.ok) {
                throw new Error('Failed to update availability status');
            }

            setStatus(newStatus);
            setIsOpen(false);
        } catch (error) {
            console.error('Failed to update availability status:', error);
            // Optionally show an error toast here
        }
    };

    return (
        <div className="relative" ref={dropdownRef}>
            <button
                onClick={() => setIsOpen(!isOpen)}
                className={`inline-flex items-center px-3 py-2 text-sm font-medium rounded-md border ${statusColors[status]} hover:opacity-80 transition-opacity`}
            >
                <span className="mr-2">{statusLabels[status]}</span>
                <ChevronDownIcon className="h-4 w-4" />
            </button>

            {isOpen && (
                <div className="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg z-50 border border-gray-200">
                    <div className="py-1">
                        {(
                            Object.keys(statusLabels) as AvailabilityStatus[]
                        ).map((statusOption) => (
                            <button
                                key={statusOption}
                                onClick={() => handleStatusChange(statusOption)}
                                className={`w-full text-left px-4 py-2 text-sm hover:bg-gray-50 ${
                                    status === statusOption
                                        ? 'bg-indigo-50 text-indigo-700'
                                        : 'text-gray-700'
                                }`}
                            >
                                {statusLabels[statusOption]}
                            </button>
                        ))}
                    </div>
                </div>
            )}
        </div>
    );
}
