'use client';

import { Dialog, DialogPanel } from '@headlessui/react';
import { CheckCircleIcon } from '@heroicons/react/24/solid';
import { useEffect } from 'react';

interface CompletionOverlayProps {
    isOpen: boolean;
    onClose: () => void;
    goalMessage: string;
}

export default function CompletionOverlay({
    isOpen,
    onClose,
    goalMessage,
}: CompletionOverlayProps) {
    // Auto-dismiss after 3 seconds
    useEffect(() => {
        if (isOpen) {
            const timer = setTimeout(() => {
                onClose();
            }, 3000);

            return () => clearTimeout(timer);
        }
    }, [isOpen, onClose]);

    return (
        <Dialog open={isOpen} onClose={onClose} className="relative z-50">
            <div className="fixed inset-0 bg-black/30" aria-hidden="true" />

            <div className="fixed inset-0 flex items-center justify-center p-4">
                <DialogPanel className="mx-auto max-w-sm rounded-lg bg-white p-8 shadow-xl">
                    <div className="text-center">
                        <CheckCircleIcon className="mx-auto h-16 w-16 text-green-500 mb-4" />
                        <h3 className="text-2xl font-bold text-gray-900 mb-2">
                            Great job! 🎉
                        </h3>
                        <p className="text-gray-600 mb-4">
                            You completed:
                        </p>
                        <p className="text-lg font-semibold text-gray-900 mb-6">
                            {goalMessage}
                        </p>
                        <button
                            onClick={onClose}
                            className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                        >
                            Continue
                        </button>
                    </div>
                </DialogPanel>
            </div>
        </Dialog>
    );
}

