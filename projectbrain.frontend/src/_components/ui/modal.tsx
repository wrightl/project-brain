'use client';

import { Dialog, DialogPanel, DialogTitle } from '@headlessui/react';
import { XMarkIcon } from '@heroicons/react/24/outline';

interface ModalProps {
    isOpen: boolean;
    onClose: () => void;
    title: string;
    description?: string;
    children: React.ReactNode;
    footer?: React.ReactNode;
    size?: 'md' | 'lg' | 'xl';
    scrollable?: boolean;
    showCloseButton?: boolean;
}

const sizeClasses = {
    md: 'max-w-lg',
    lg: 'max-w-2xl',
    xl: 'max-w-4xl',
};

export default function Modal({
    isOpen,
    onClose,
    title,
    description,
    children,
    footer,
    size = 'lg',
    scrollable = false,
    showCloseButton = true,
}: ModalProps) {
    const panelClasses = [
        'w-full',
        sizeClasses[size],
        'transform rounded-lg bg-white text-left shadow transition-all',
        scrollable
            ? 'flex max-h-[90vh] flex-col overflow-hidden'
            : 'overflow-hidden p-6',
    ].join(' ');

    return (
        <Dialog
            open={isOpen}
            onClose={onClose}
            className="relative z-50"
            aria-labelledby="modal-title"
            aria-describedby={description ? 'modal-description' : undefined}
        >
            <div
                className="fixed inset-0 bg-gray-500 bg-opacity-75"
                aria-hidden="true"
            />
            <div className="fixed inset-0 z-50 overflow-y-auto p-4">
                <div className="flex min-h-full items-center justify-center">
                    <DialogPanel className={panelClasses}>
                        <div
                            className={
                                scrollable
                                    ? 'flex shrink-0 items-start justify-between gap-4 border-b border-gray-200 px-6 py-4'
                                    : 'flex items-start justify-between gap-4'
                            }
                        >
                            <div>
                                <DialogTitle
                                    id="modal-title"
                                    className="text-lg font-medium text-gray-900"
                                >
                                    {title}
                                </DialogTitle>
                                {description ? (
                                    <p
                                        id="modal-description"
                                        className="mt-1 text-sm text-gray-600"
                                    >
                                        {description}
                                    </p>
                                ) : null}
                            </div>
                            {showCloseButton ? (
                                <button
                                    type="button"
                                    onClick={onClose}
                                    className="text-gray-400 hover:text-gray-500"
                                    aria-label="Close dialog"
                                >
                                    <XMarkIcon className="h-6 w-6" />
                                </button>
                            ) : null}
                        </div>
                        <div
                            className={
                                scrollable
                                    ? 'flex-1 overflow-y-auto px-6 py-4'
                                    : 'mt-4'
                            }
                        >
                            {children}
                        </div>
                        {footer ? (
                            <div
                                className={
                                    scrollable
                                        ? 'shrink-0 border-t border-gray-200 bg-gray-50 px-6 py-4'
                                        : 'mt-4 border-t border-gray-200 pt-4'
                                }
                            >
                                {footer}
                            </div>
                        ) : null}
                    </DialogPanel>
                </div>
            </div>
        </Dialog>
    );
}
