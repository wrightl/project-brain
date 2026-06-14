'use client';

import { CheckCircleIcon } from '@heroicons/react/24/solid';
import { useEffect } from 'react';
import Modal from '@/_components/ui/modal';

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
    useEffect(() => {
        if (isOpen) {
            const timer = setTimeout(() => {
                onClose();
            }, 3000);

            return () => clearTimeout(timer);
        }
    }, [isOpen, onClose]);

    return (
        <Modal
            isOpen={isOpen}
            onClose={onClose}
            title="Great job!"
            size="md"
            showCloseButton={false}
        >
            <div className="text-center">
                <CheckCircleIcon className="mx-auto mb-4 h-16 w-16 text-green-500" />
                <p className="mb-4 text-gray-600">You completed:</p>
                <p className="mb-6 text-lg font-semibold text-gray-900">
                    {goalMessage}
                </p>
                <button
                    type="button"
                    onClick={onClose}
                    className="inline-flex items-center rounded-md border border-transparent bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
                >
                    Continue
                </button>
            </div>
        </Modal>
    );
}
