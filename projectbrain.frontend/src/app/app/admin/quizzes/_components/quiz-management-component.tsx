'use client';

import { Quiz } from '@/_services/quiz-service';
import { useEffect, useState } from 'react';
import {
    PencilIcon,
    TrashIcon,
    PlusIcon,
    DocumentTextIcon,
} from '@heroicons/react/24/outline';
import QuizFormModal from './quiz-form-modal';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import toast from 'react-hot-toast';
import ConfirmationDialog from '@/_components/confirmation-dialog';

export default function QuizManagementComponent() {
    const [quizzes, setQuizzes] = useState<Quiz[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [selectedQuiz, setSelectedQuiz] = useState<Quiz | null>(null);
    const [isFormModalOpen, setIsFormModalOpen] = useState(false);
    const [searchQuery, setSearchQuery] = useState<string>('');
    const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
    const [quizToDelete, setQuizToDelete] = useState<Quiz | null>(null);

    useEffect(() => {
        loadQuizzes();
    }, []);

    const loadQuizzes = async () => {
        try {
            setLoading(true);
            setError(null);
            const response = await fetchWithAuth('/api/admin/quizzes');
            if (!response.ok) {
                throw new Error('Failed to load quizzes');
            }
            const data = await response.json();
            setQuizzes(data.items || []);
        } catch (err) {
            setError(
                err instanceof Error ? err.message : 'Failed to load quizzes'
            );
            console.error('Error loading quizzes:', err);
        } finally {
            setLoading(false);
        }
    };

    const handleCreate = () => {
        setSelectedQuiz(null);
        setIsFormModalOpen(true);
    };

    const handleEdit = async (quiz: Quiz) => {
        try {
            // Load full quiz with questions
            const response = await fetchWithAuth(
                `/api/admin/quizzes/${quiz.id}`
            );
            if (!response.ok) {
                throw new Error('Failed to load quiz details');
            }
            const fullQuiz = await response.json();
            setSelectedQuiz(fullQuiz);
            setIsFormModalOpen(true);
        } catch (err) {
            toast.error(
                err instanceof Error
                    ? err.message
                    : 'Failed to load quiz details'
            );
            console.error('Error loading quiz:', err);
        }
    };

    const handleDeleteClick = (quiz: Quiz) => {
        setQuizToDelete(quiz);
        setDeleteConfirmOpen(true);
    };

    const handleDelete = async () => {
        if (!quizToDelete) return;

        try {
            const response = await fetchWithAuth(
                `/api/admin/quizzes/${quizToDelete.id}`,
                {
                    method: 'DELETE',
                }
            );

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(
                    errorData.error?.message || 'Failed to delete quiz'
                );
            }

            toast.success('Quiz deleted successfully');
            await loadQuizzes();
        } catch (err) {
            toast.error(err instanceof Error ? err.message : 'Failed to delete quiz');
            console.error('Error deleting quiz:', err);
        } finally {
            setDeleteConfirmOpen(false);
            setQuizToDelete(null);
        }
    };

    const handleFormClose = () => {
        setIsFormModalOpen(false);
        setSelectedQuiz(null);
    };

    const handleFormSuccess = () => {
        handleFormClose();
        loadQuizzes();
    };

    const filteredQuizzes = quizzes.filter((quiz) => {
        if (!searchQuery) return true;
        const query = searchQuery.toLowerCase();
        return (
            quiz.title.toLowerCase().includes(query) ||
            (quiz.description && quiz.description.toLowerCase().includes(query))
        );
    });

    if (loading) {
        return (
            <div className="flex justify-center items-center h-64">
                <div className="text-gray-500">Loading quizzes...</div>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h1 className="text-3xl font-bold text-gray-900">
                        Quiz Management
                    </h1>
                    <p className="mt-2 text-sm text-gray-600">
                        Create and manage neurodiversity assessment quizzes
                    </p>
                </div>
                <button
                    onClick={handleCreate}
                    className="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
                >
                    <PlusIcon className="h-5 w-5 mr-2" />
                    Create Quiz
                </button>
            </div>

            {error && (
                <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
                    {error}
                </div>
            )}

            {/* Search */}
            <div className="bg-white shadow rounded-lg p-4">
                <input
                    type="text"
                    placeholder="Search quizzes by title or description..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-indigo-500 focus:border-indigo-500"
                />
            </div>

            {/* Quizzes List */}
            <div className="bg-white shadow overflow-hidden sm:rounded-md">
                <ul className="divide-y divide-gray-200">
                    {filteredQuizzes.length === 0 ? (
                        <li className="px-6 py-12 text-center">
                            <DocumentTextIcon className="mx-auto h-12 w-12 text-gray-400" />
                            <h3 className="mt-2 text-sm font-medium text-gray-900">
                                No quizzes
                            </h3>
                            <p className="mt-1 text-sm text-gray-500">
                                Get started by creating a new quiz.
                            </p>
                        </li>
                    ) : (
                        filteredQuizzes.map((quiz) => (
                            <li key={quiz.id} className="px-6 py-4">
                                <div className="flex items-center justify-between">
                                    <div className="flex-1 min-w-0">
                                        <div className="flex items-center">
                                            <h3 className="text-lg font-medium text-gray-900 truncate">
                                                {quiz.title}
                                            </h3>
                                        </div>
                                        {quiz.description && (
                                            <p className="mt-1 text-sm text-gray-500 line-clamp-2">
                                                {quiz.description}
                                            </p>
                                        )}
                                        <div className="mt-2 flex items-center text-xs text-gray-500">
                                            <span>
                                                Created:{' '}
                                                {new Date(
                                                    quiz.createdAt
                                                ).toLocaleDateString()}
                                            </span>
                                            {quiz.questions && (
                                                <span className="ml-4">
                                                    {quiz.questions.length}{' '}
                                                    question
                                                    {quiz.questions.length !== 1
                                                        ? 's'
                                                        : ''}
                                                </span>
                                            )}
                                        </div>
                                    </div>
                                    <div className="ml-4 flex items-center space-x-2">
                                        <button
                                            onClick={() => handleEdit(quiz)}
                                            className="inline-flex items-center px-3 py-2 border border-gray-300 shadow-sm text-sm leading-4 font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
                                        >
                                            <PencilIcon className="h-4 w-4 mr-1" />
                                            Edit
                                        </button>
                                        <button
                                            onClick={() => handleDeleteClick(quiz)}
                                            className="inline-flex items-center px-3 py-2 border border-red-300 shadow-sm text-sm leading-4 font-medium rounded-md text-red-700 bg-white hover:bg-red-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500"
                                        >
                                            <TrashIcon className="h-4 w-4 mr-1" />
                                            Delete
                                        </button>
                                    </div>
                                </div>
                            </li>
                        ))
                    )}
                </ul>
            </div>

            {/* Quiz Form Modal */}
            {isFormModalOpen && (
                <QuizFormModal
                    quiz={selectedQuiz}
                    onClose={handleFormClose}
                    onSuccess={handleFormSuccess}
                />
            )}

            {/* Delete Confirmation Dialog */}
            <ConfirmationDialog
                isOpen={deleteConfirmOpen}
                onClose={() => {
                    setDeleteConfirmOpen(false);
                    setQuizToDelete(null);
                }}
                onConfirm={handleDelete}
                title="Delete Quiz"
                message={
                    quizToDelete
                        ? `Are you sure you want to delete "${quizToDelete.title}"? This action cannot be undone.`
                        : ''
                }
                confirmText="Delete"
                cancelText="Cancel"
                variant="danger"
            />
        </div>
    );
}
