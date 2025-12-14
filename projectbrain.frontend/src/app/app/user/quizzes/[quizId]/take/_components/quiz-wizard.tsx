'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useQuiz, useSubmitQuizResponse } from '@/_hooks/queries/use-quizzes';
import { QuizQuestion } from '@/_services/quiz-service';
import {
    ChevronLeftIcon,
    ChevronRightIcon,
    CheckCircleIcon,
} from '@heroicons/react/24/outline';
import toast from 'react-hot-toast';

interface QuizWizardProps {
    quizId: string;
}

export default function QuizWizard({ quizId }: QuizWizardProps) {
    const router = useRouter();
    const { data: quiz, isLoading, error } = useQuiz(quizId);
    const submitMutation = useSubmitQuizResponse();

    const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
    const [answers, setAnswers] = useState<Record<string, unknown>>({});
    const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

    const visibleQuestions = quiz?.questions?.filter(q => q.visible) || [];
    const sortedQuestions = [...visibleQuestions].sort((a, b) => {
        // Sort by question order if available, otherwise by index
        return 0; // Questions should already be sorted from the API
    });

    const currentQuestion = sortedQuestions[currentQuestionIndex];
    const isFirstQuestion = currentQuestionIndex === 0;
    const isLastQuestion = currentQuestionIndex === sortedQuestions.length - 1;

    const validateCurrentQuestion = (): boolean => {
        if (!currentQuestion) return false;

        const questionId = currentQuestion.id || '';
        const answer = answers[questionId];

        // Clear previous error
        const newErrors = { ...validationErrors };
        delete newErrors[questionId];

        // Check mandatory
        if (currentQuestion.mandatory) {
            if (answer === undefined || answer === null || answer === '') {
                newErrors[questionId] = 'This question is mandatory';
                setValidationErrors(newErrors);
                return false;
            }

            // Check if it's an array and empty
            if (Array.isArray(answer) && answer.length === 0) {
                newErrors[questionId] = 'This question is mandatory';
                setValidationErrors(newErrors);
                return false;
            }
        }

        setValidationErrors(newErrors);
        return true;
    };

    const handleAnswerChange = (questionId: string, value: unknown) => {
        setAnswers(prev => ({
            ...prev,
            [questionId]: value,
        }));

        // Clear validation error when user starts typing
        if (validationErrors[questionId]) {
            const newErrors = { ...validationErrors };
            delete newErrors[questionId];
            setValidationErrors(newErrors);
        }
    };

    const handleNext = () => {
        if (validateCurrentQuestion()) {
            if (!isLastQuestion) {
                setCurrentQuestionIndex(prev => prev + 1);
            }
        }
    };

    const handlePrevious = () => {
        if (!isFirstQuestion) {
            setCurrentQuestionIndex(prev => prev - 1);
        }
    };

    const handleSubmit = async () => {
        if (!validateCurrentQuestion()) {
            return;
        }

        try {
            await submitMutation.mutateAsync({
                quizId,
                answers,
            });
            toast.success('Quiz submitted successfully!');
            router.push('/app/user/quizzes/responses');
        } catch (error) {
            toast.error(
                error instanceof Error
                    ? error.message
                    : 'Failed to submit quiz'
            );
        }
    };

    const renderQuestionInput = (question: QuizQuestion) => {
        const questionId = question.id || '';
        const value = answers[questionId];
        const error = validationErrors[questionId];

        switch (question.inputType) {
            case 'text':
            case 'email':
            case 'tel':
            case 'url':
                return (
                    <input
                        type={question.inputType}
                        value={(value as string) || ''}
                        onChange={(e) => handleAnswerChange(questionId, e.target.value)}
                        placeholder={question.placeholder}
                        className={`mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm ${
                            error ? 'border-red-300' : ''
                        }`}
                    />
                );

            case 'textarea':
                return (
                    <textarea
                        value={(value as string) || ''}
                        onChange={(e) => handleAnswerChange(questionId, e.target.value)}
                        placeholder={question.placeholder}
                        rows={4}
                        className={`mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm ${
                            error ? 'border-red-300' : ''
                        }`}
                    />
                );

            case 'number':
            case 'scale':
                return (
                    <input
                        type="number"
                        value={(value as number) || ''}
                        onChange={(e) => handleAnswerChange(questionId, e.target.value ? parseFloat(e.target.value) : '')}
                        min={question.minValue}
                        max={question.maxValue}
                        placeholder={question.placeholder}
                        className={`mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm ${
                            error ? 'border-red-300' : ''
                        }`}
                    />
                );

            case 'date':
                return (
                    <input
                        type="date"
                        value={(value as string) || ''}
                        onChange={(e) => handleAnswerChange(questionId, e.target.value)}
                        className={`mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm ${
                            error ? 'border-red-300' : ''
                        }`}
                    />
                );

            case 'choice':
                return (
                    <div className="mt-1 space-y-2">
                        {question.choices?.map((choice) => (
                            <label
                                key={choice}
                                className="flex items-center"
                            >
                                <input
                                    type="radio"
                                    name={questionId}
                                    value={choice}
                                    checked={value === choice}
                                    onChange={(e) => handleAnswerChange(questionId, e.target.value)}
                                    className="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300"
                                />
                                <span className="ml-2 text-sm text-gray-700">
                                    {choice}
                                </span>
                            </label>
                        ))}
                    </div>
                );

            case 'multipleChoice':
                const selectedChoices = (value as string[]) || [];
                return (
                    <div className="mt-1 space-y-2">
                        {question.choices?.map((choice) => (
                            <label
                                key={choice}
                                className="flex items-center"
                            >
                                <input
                                    type="checkbox"
                                    checked={selectedChoices.includes(choice)}
                                    onChange={(e) => {
                                        const newChoices = e.target.checked
                                            ? [...selectedChoices, choice]
                                            : selectedChoices.filter(c => c !== choice);
                                        handleAnswerChange(questionId, newChoices);
                                    }}
                                    className="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300 rounded"
                                />
                                <span className="ml-2 text-sm text-gray-700">
                                    {choice}
                                </span>
                            </label>
                        ))}
                    </div>
                );

            default:
                return (
                    <input
                        type="text"
                        value={(value as string) || ''}
                        onChange={(e) => handleAnswerChange(questionId, e.target.value)}
                        placeholder={question.placeholder}
                        className={`mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm ${
                            error ? 'border-red-300' : ''
                        }`}
                    />
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

    if (error || !quiz) {
        return (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                <p className="text-sm text-red-800">
                    {error instanceof Error
                        ? error.message
                        : 'Failed to load quiz'}
                </p>
            </div>
        );
    }

    if (sortedQuestions.length === 0) {
        return (
            <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
                <p className="text-sm text-yellow-800">
                    This quiz has no questions available.
                </p>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            {/* Quiz Header */}
            <div>
                <h1 className="text-2xl font-bold text-gray-900">{quiz.title}</h1>
                {quiz.description && (
                    <p className="mt-2 text-sm text-gray-600">{quiz.description}</p>
                )}
            </div>

            {/* Progress Indicator */}
            <div className="bg-white rounded-lg shadow p-4">
                <div className="flex items-center justify-between mb-2">
                    <span className="text-sm font-medium text-gray-700">
                        Question {currentQuestionIndex + 1} of {sortedQuestions.length}
                    </span>
                    <span className="text-sm text-gray-500">
                        {Math.round(((currentQuestionIndex + 1) / sortedQuestions.length) * 100)}%
                    </span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                    <div
                        className="bg-indigo-600 h-2 rounded-full transition-all duration-300"
                        style={{
                            width: `${((currentQuestionIndex + 1) / sortedQuestions.length) * 100}%`,
                        }}
                    />
                </div>
            </div>

            {/* Question Card */}
            <div className="bg-white rounded-lg shadow p-6">
                {currentQuestion && (
                    <div className="space-y-4">
                        <div>
                            <label className="block text-sm font-medium text-gray-700">
                                {currentQuestion.label}
                                {currentQuestion.mandatory && (
                                    <span className="text-red-500 ml-1">*</span>
                                )}
                            </label>
                            {currentQuestion.hint && (
                                <p className="mt-1 text-sm text-gray-500">
                                    {currentQuestion.hint}
                                </p>
                            )}
                        </div>

                        {renderQuestionInput(currentQuestion)}

                        {validationErrors[currentQuestion.id || ''] && (
                            <p className="mt-1 text-sm text-red-600">
                                {validationErrors[currentQuestion.id || '']}
                            </p>
                        )}
                    </div>
                )}
            </div>

            {/* Navigation Buttons */}
            <div className="flex items-center justify-between">
                <button
                    onClick={handlePrevious}
                    disabled={isFirstQuestion}
                    className="inline-flex items-center px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                    <ChevronLeftIcon className="h-5 w-5 mr-1" />
                    Previous
                </button>

                {isLastQuestion ? (
                    <button
                        onClick={handleSubmit}
                        disabled={submitMutation.isPending}
                        className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                        <CheckCircleIcon className="h-5 w-5 mr-1" />
                        {submitMutation.isPending ? 'Submitting...' : 'Submit Quiz'}
                    </button>
                ) : (
                    <button
                        onClick={handleNext}
                        className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
                    >
                        Next
                        <ChevronRightIcon className="h-5 w-5 ml-1" />
                    </button>
                )}
            </div>
        </div>
    );
}

