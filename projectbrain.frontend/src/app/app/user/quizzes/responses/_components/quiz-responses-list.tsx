'use client';

import { useState } from 'react';
import Link from 'next/link';
import {
    useQuizResponses,
    useDeleteQuizResponse,
    useQuizInsights,
} from '@/_hooks/queries/use-quizzes';
import { QuizResponse } from '@/_services/quiz-service';
import {
    TrashIcon,
    PlusIcon,
    ClockIcon,
    AcademicCapIcon,
} from '@heroicons/react/24/outline';
import toast from 'react-hot-toast';
import ConfirmationDialog from '@/_components/confirmation-dialog';

export default function QuizResponsesList() {
    const [page, setPage] = useState(1);
    const pageSize = 20;
    const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
    const [responseToDelete, setResponseToDelete] = useState<string | null>(null);

    const {
        data: responsesResponse,
        isLoading,
        error,
    } = useQuizResponses({ page, pageSize });

    const {
        data: quizInsights,
        isLoading: insightsLoading,
    } = useQuizInsights();

    const deleteMutation = useDeleteQuizResponse();

    const responses = responsesResponse?.items || [];
    const totalPages = responsesResponse?.totalPages || 0;

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-GB', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        });
    };

    const handleDeleteClick = (id: string) => {
        setResponseToDelete(id);
        setDeleteConfirmOpen(true);
    };

    const handleDelete = async () => {
        if (!responseToDelete) return;

        try {
            await deleteMutation.mutateAsync(responseToDelete);
            toast.success('Quiz response deleted successfully');
            setDeleteConfirmOpen(false);
            setResponseToDelete(null);
        } catch (error) {
            toast.error(
                error instanceof Error
                    ? error.message
                    : 'Failed to delete quiz response'
            );
        }
    };

    if (isLoading) {
        return (
            <div className="flex justify-center items-center py-12">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600" />
            </div>
        );
    }

    if (error) {
        return (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                <p className="text-sm text-red-800">
                    {error instanceof Error
                        ? error.message
                        : 'Failed to load quiz responses'}
                </p>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <h2 className="text-lg font-semibold text-gray-900">
                    Your Quiz Responses
                </h2>
                <Link
                    href="/app/user/quizzes"
                    className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
                >
                    <PlusIcon className="h-5 w-5 mr-2" />
                    Take New Quiz
                </Link>
            </div>

            {/* Quiz Insights */}
            {!insightsLoading && quizInsights && (
                <div>
                    <h2 className="text-lg font-semibold text-gray-900 mb-4">
                        Quiz Insights
                    </h2>
                    <div className="bg-white shadow rounded-lg p-6">
                        <p className="text-sm text-gray-700 mb-4">
                            {quizInsights.summary}
                        </p>
                        {quizInsights.keyInsights &&
                            quizInsights.keyInsights.length > 0 && (
                                <ul className="space-y-2">
                                    {quizInsights.keyInsights.map(
                                        (insight, index) => (
                                            <li
                                                key={index}
                                                className="text-sm text-gray-600 flex items-start"
                                            >
                                                <span className="text-indigo-600 mr-2">
                                                    •
                                                </span>
                                                {insight}
                                            </li>
                                        )
                                    )}
                                </ul>
                            )}
                    </div>
                </div>
            )}

            {responses.length === 0 ? (
                <div className="text-center py-12 bg-white rounded-lg shadow">
                    <AcademicCapIcon className="mx-auto h-12 w-12 text-gray-400" />
                    <p className="text-gray-500 text-lg mt-4">No quiz responses yet</p>
                    <p className="text-gray-400 text-sm mt-2">
                        Start by taking your first quiz
                    </p>
                    <Link
                        href="/app/user/quizzes"
                        className="mt-4 inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
                    >
                        <PlusIcon className="h-5 w-5 mr-2" />
                        Take Your First Quiz
                    </Link>
                </div>
            ) : (
                <>
                    <div className="bg-white shadow rounded-lg overflow-hidden">
                        <ul className="divide-y divide-gray-200">
                            {responses.map((response: QuizResponse) => (
                                <li key={response.id} className="p-6 hover:bg-gray-50">
                                    <div className="flex items-start justify-between">
                                        <div className="flex-1 min-w-0">
                                            <Link
                                                href={`/app/user/quizzes/responses/${response.id}`}
                                                className="block"
                                            >
                                                <div className="flex items-center">
                                                    <AcademicCapIcon className="h-5 w-5 text-gray-400 mr-3 flex-shrink-0" />
                                                    <div className="flex-1 min-w-0">
                                                        <p className="text-sm font-medium text-gray-900 truncate">
                                                            {response.quizTitle || 'Untitled Quiz'}
                                                        </p>
                                                        <div className="mt-2 flex items-center text-sm text-gray-500">
                                                            <ClockIcon className="h-4 w-4 mr-1" />
                                                            Completed: {formatDate(response.completedAt)}
                                                        </div>
                                                        {response.score !== null && response.score !== undefined && (
                                                            <div className="mt-1 text-sm text-gray-600">
                                                                Score: {response.score.toFixed(1)}%
                                                            </div>
                                                        )}
                                                    </div>
                                                </div>
                                            </Link>
                                        </div>
                                        <div className="ml-4 flex items-center space-x-2">
                                            <button
                                                onClick={(e) => {
                                                    e.preventDefault();
                                                    handleDeleteClick(response.id);
                                                }}
                                                className="text-gray-400 hover:text-red-600"
                                                title="Delete"
                                                disabled={deleteMutation.isPending}
                                            >
                                                <TrashIcon className="h-5 w-5" />
                                            </button>
                                        </div>
                                    </div>
                                </li>
                            ))}
                        </ul>
                    </div>

                    {/* Pagination */}
                    {totalPages > 1 && (
                        <div className="flex items-center justify-between border-t border-gray-200 bg-white px-4 py-3 sm:px-6">
                            <div className="flex flex-1 justify-between sm:hidden">
                                <button
                                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                                    disabled={page === 1}
                                    className="relative inline-flex items-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                                >
                                    Previous
                                </button>
                                <button
                                    onClick={() =>
                                        setPage((p) =>
                                            Math.min(totalPages, p + 1)
                                        )
                                    }
                                    disabled={page === totalPages}
                                    className="relative ml-3 inline-flex items-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                                >
                                    Next
                                </button>
                            </div>
                            <div className="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
                                <div>
                                    <p className="text-sm text-gray-700">
                                        Page{' '}
                                        <span className="font-medium">
                                            {page}
                                        </span>{' '}
                                        of{' '}
                                        <span className="font-medium">
                                            {totalPages}
                                        </span>
                                    </p>
                                </div>
                                <div>
                                    <nav
                                        className="isolate inline-flex -space-x-px rounded-md shadow-sm"
                                        aria-label="Pagination"
                                    >
                                        <button
                                            onClick={() =>
                                                setPage((p) => Math.max(1, p - 1))
                                            }
                                            disabled={page === 1}
                                            className="relative inline-flex items-center rounded-l-md px-2 py-2 text-gray-400 ring-1 ring-inset ring-gray-300 hover:bg-gray-50 focus:z-20 focus:outline-offset-0 disabled:opacity-50 disabled:cursor-not-allowed"
                                        >
                                            Previous
                                        </button>
                                        <button
                                            onClick={() =>
                                                setPage((p) =>
                                                    Math.min(totalPages, p + 1)
                                                )
                                            }
                                            disabled={page === totalPages}
                                            className="relative inline-flex items-center rounded-r-md px-2 py-2 text-gray-400 ring-1 ring-inset ring-gray-300 hover:bg-gray-50 focus:z-20 focus:outline-offset-0 disabled:opacity-50 disabled:cursor-not-allowed"
                                        >
                                            Next
                                        </button>
                                    </nav>
                                </div>
                            </div>
                        </div>
                    )}
                </>
            )}

            {/* Delete Confirmation Dialog */}
            <ConfirmationDialog
                isOpen={deleteConfirmOpen}
                onClose={() => {
                    setDeleteConfirmOpen(false);
                    setResponseToDelete(null);
                }}
                onConfirm={handleDelete}
                title="Delete Quiz Response"
                message="Are you sure you want to delete this quiz response? This action cannot be undone."
                confirmText="Delete"
                variant="danger"
            />
        </div>
    );
}

