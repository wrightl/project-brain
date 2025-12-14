'use client';

import { useQuizResponse, useQuiz } from '@/_hooks/queries/use-quizzes';
import { QuizQuestion } from '@/_services/quiz-service';
import { AcademicCapIcon, ClockIcon } from '@heroicons/react/24/outline';

interface QuizResponseViewProps {
    responseId: string;
}

export default function QuizResponseView({ responseId }: QuizResponseViewProps) {
    const { data: response, isLoading: responseLoading, error: responseError } = useQuizResponse(responseId);
    const { data: quiz, isLoading: quizLoading, error: quizError } = useQuiz(response?.quizId || '');

    const isLoading = responseLoading || quizLoading;
    const error = responseError || quizError;

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

    const getQuestionById = (questionId: string): QuizQuestion | undefined => {
        return quiz?.questions?.find(q => q.id === questionId);
    };

    const formatAnswer = (question: QuizQuestion | undefined, answer: unknown): string => {
        if (answer === null || answer === undefined) {
            return 'Not answered';
        }

        if (Array.isArray(answer)) {
            return answer.join(', ');
        }

        if (typeof answer === 'object') {
            return JSON.stringify(answer);
        }

        return String(answer);
    };

    if (isLoading) {
        return (
            <div className="flex justify-center items-center py-12">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600" />
            </div>
        );
    }

    if (error || !response) {
        return (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                <p className="text-sm text-red-800">
                    {error instanceof Error
                        ? error.message
                        : 'Failed to load quiz response'}
                </p>
            </div>
        );
    }

    const visibleQuestions = quiz?.questions?.filter(q => q.visible).sort((a, b) => {
        // Sort by question order - assuming they're already in order from API
        return 0;
    }) || [];

    return (
        <div className="space-y-6">
            {/* Header */}
            <div>
                <h1 className="text-2xl font-bold text-gray-900">
                    {quiz?.title || 'Quiz Response'}
                </h1>
                {quiz?.description && (
                    <p className="mt-2 text-sm text-gray-600">{quiz.description}</p>
                )}
            </div>

            {/* Summary Info */}
            <div className="bg-white rounded-lg shadow p-6">
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                    <div className="flex items-center">
                        <ClockIcon className="h-5 w-5 text-gray-400 mr-2" />
                        <div>
                            <p className="text-sm text-gray-500">Completed</p>
                            <p className="text-sm font-medium text-gray-900">
                                {formatDate(response.completedAt)}
                            </p>
                        </div>
                    </div>
                    {response.score !== null && response.score !== undefined && (
                        <div className="flex items-center">
                            <AcademicCapIcon className="h-5 w-5 text-gray-400 mr-2" />
                            <div>
                                <p className="text-sm text-gray-500">Score</p>
                                <p className="text-sm font-medium text-gray-900">
                                    {response.score.toFixed(1)}%
                                </p>
                            </div>
                        </div>
                    )}
                </div>
            </div>

            {/* Questions and Answers */}
            <div className="space-y-4">
                <h2 className="text-lg font-semibold text-gray-900">Your Answers</h2>
                {visibleQuestions.map((question, index) => {
                    const questionId = question.id || '';
                    const answer = response.answers?.[questionId];
                    return (
                        <div key={questionId} className="bg-white rounded-lg shadow p-6">
                            <div className="space-y-2">
                                <div className="flex items-start">
                                    <span className="text-sm font-medium text-gray-500 mr-2">
                                        {index + 1}.
                                    </span>
                                    <div className="flex-1">
                                        <h3 className="text-sm font-medium text-gray-900">
                                            {question.label}
                                        </h3>
                                        {question.mandatory && (
                                            <span className="ml-1 text-xs text-red-500">*</span>
                                        )}
                                    </div>
                                </div>
                                <div className="ml-6 mt-2">
                                    <p className="text-sm text-gray-700 bg-gray-50 rounded-md p-3">
                                        {formatAnswer(question, answer)}
                                    </p>
                                </div>
                            </div>
                        </div>
                    );
                })}
            </div>
        </div>
    );
}

