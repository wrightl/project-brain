'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useQuizzes } from '@/_hooks/queries/use-quizzes';
import { Quiz } from '@/_services/quiz-service';
import { AcademicCapIcon } from '@heroicons/react/24/outline';

export default function QuizList() {
    const [page, setPage] = useState(1);
    const pageSize = 20;

    const {
        data: quizzesResponse,
        isLoading,
        error,
    } = useQuizzes({ page, pageSize });

    const quizzes = quizzesResponse?.items || [];
    const totalPages = quizzesResponse?.totalPages || 0;

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
                        : 'Failed to load quizzes'}
                </p>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <h2 className="text-lg font-semibold text-gray-900">
                    Available Quizzes
                </h2>
            </div>

            {quizzes.length === 0 ? (
                <div className="text-center py-12 bg-white rounded-lg shadow">
                    <AcademicCapIcon className="mx-auto h-12 w-12 text-gray-400" />
                    <p className="text-gray-500 text-lg mt-4">No quizzes available</p>
                    <p className="text-gray-400 text-sm mt-2">
                        Check back later for new quizzes
                    </p>
                </div>
            ) : (
                <>
                    <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
                        {quizzes.map((quiz: Quiz) => (
                            <Link
                                key={quiz.id}
                                href={`/app/user/quizzes/${quiz.id}/take`}
                                className="block bg-white rounded-lg shadow hover:shadow-lg transition-shadow p-6"
                            >
                                <div className="flex items-start">
                                    <AcademicCapIcon className="h-6 w-6 text-indigo-600 mr-3 flex-shrink-0 mt-1" />
                                    <div className="flex-1 min-w-0">
                                        <h3 className="text-lg font-semibold text-gray-900">
                                            {quiz.title}
                                        </h3>
                                        {quiz.description && (
                                            <p className="mt-2 text-sm text-gray-600 line-clamp-3">
                                                {quiz.description}
                                            </p>
                                        )}
                                        <div className="mt-4">
                                            <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-indigo-100 text-indigo-800">
                                                Take Quiz
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            </Link>
                        ))}
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
        </div>
    );
}

