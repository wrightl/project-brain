'use client';

import Link from 'next/link';
import {
    useCopingStrategyLibrary,
    useDeleteCopingStrategy,
    useRateCopingStrategy,
} from '@/_hooks/queries/use-coping-strategies';
import StarRating from '@/_components/coach/star-rating';
import { TrashIcon } from '@heroicons/react/24/outline';
import toast from 'react-hot-toast';
import { useState } from 'react';
import ConfirmationDialog from '@/_components/confirmation-dialog';
import { SkeletonList } from '@/_components/ui/skeleton';

export default function StrategiesLibrary() {
    const { data, isLoading, error } = useCopingStrategyLibrary();
    const deleteMutation = useDeleteCopingStrategy();
    const rateMutation = useRateCopingStrategy();
    const [busyIds, setBusyIds] = useState<Set<string>>(new Set());
    const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
    const [strategyToDelete, setStrategyToDelete] = useState<string | null>(null);

    if (isLoading) {
        return <SkeletonList count={4} />;
    }

    if (error) {
        return (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                <p className="text-sm text-red-800">
                    {error instanceof Error
                        ? error.message
                        : 'Failed to load coping strategies'}
                </p>
            </div>
        );
    }

    const items = data?.items ?? [];

    const setBusy = (id: string, busy: boolean) => {
        setBusyIds((prev) => {
            const next = new Set(prev);
            if (busy) next.add(id);
            else next.delete(id);
            return next;
        });
    };

    const handleDeleteClick = (id: string) => {
        setStrategyToDelete(id);
        setDeleteConfirmOpen(true);
    };

    const handleDelete = async () => {
        if (!strategyToDelete) return;

        try {
            setBusy(strategyToDelete, true);
            await deleteMutation.mutateAsync(strategyToDelete);
            toast.success('Strategy deleted');
        } catch (err) {
            toast.error(
                err instanceof Error ? err.message : 'Failed to delete strategy'
            );
        } finally {
            setBusy(strategyToDelete, false);
            setStrategyToDelete(null);
        }
    };

    const handleRate = async (id: string, rating: number) => {
        try {
            setBusy(id, true);
            await rateMutation.mutateAsync({ id, rating });
            toast.success('Rating saved');
        } catch (err) {
            toast.error(
                err instanceof Error ? err.message : 'Failed to save rating'
            );
        } finally {
            setBusy(id, false);
        }
    };

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h2 className="text-lg font-semibold text-gray-900">
                    Your library
                </h2>
                <Link
                    href="/app/user/chat/strategies"
                    className="inline-flex items-center rounded-md bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700"
                >
                    Get new strategies
                </Link>
            </div>

            {items.length === 0 ? (
                <div className="bg-white shadow rounded-lg p-8 text-center">
                    <p className="text-gray-500 text-lg">
                        No strategies saved yet
                    </p>
                    <p className="text-gray-400 text-sm mt-2">
                        Start by chatting to get some suggestions.
                    </p>
                    <Link
                        href="/app/user/chat/strategies"
                        className="mt-4 inline-flex items-center rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
                    >
                        Get strategies
                    </Link>
                </div>
            ) : (
                <div className="bg-white shadow rounded-lg overflow-hidden">
                    <ul className="divide-y divide-gray-200">
                        {items.map((strategy) => (
                            <li key={strategy.id} className="p-6">
                                <div className="flex items-start justify-between gap-4">
                                    <div className="min-w-0">
                                        <p className="text-sm font-semibold text-gray-900">
                                            {strategy.title}
                                        </p>
                                        <p className="mt-1 text-sm text-gray-600">
                                            {strategy.description}
                                        </p>

                                        <div className="mt-3 flex items-center gap-3">
                                            <span className="text-xs font-medium text-gray-500">
                                                Rate:
                                            </span>
                                            <StarRating
                                                rating={strategy.rating ?? 0}
                                                interactive={true}
                                                onRatingChange={(r) =>
                                                    handleRate(strategy.id, r)
                                                }
                                                size="sm"
                                            />
                                        </div>
                                    </div>

                                    <button
                                        type="button"
                                        onClick={() => handleDeleteClick(strategy.id)}
                                        disabled={
                                            busyIds.has(strategy.id) ||
                                            deleteMutation.isPending ||
                                            rateMutation.isPending
                                        }
                                        className="inline-flex items-center rounded-md border border-gray-300 bg-white p-2 text-gray-500 hover:text-red-600 hover:border-red-300 disabled:opacity-50"
                                        title="Delete"
                                        aria-label="Delete strategy"
                                    >
                                        <TrashIcon className="h-5 w-5" />
                                    </button>
                                </div>
                            </li>
                        ))}
                    </ul>
                </div>
            )}
            <ConfirmationDialog
                isOpen={deleteConfirmOpen}
                onClose={() => {
                    setDeleteConfirmOpen(false);
                    setStrategyToDelete(null);
                }}
                onConfirm={handleDelete}
                title="Delete strategy"
                message="Are you sure you want to delete this strategy?"
                confirmText="Delete"
                variant="danger"
            />
        </div>
    );
}

