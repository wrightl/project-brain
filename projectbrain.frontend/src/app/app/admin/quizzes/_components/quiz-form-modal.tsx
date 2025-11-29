'use client';

import { Quiz, QuizQuestion } from '@/_services/quiz-service';
import { useState, useEffect } from 'react';
import {
    XMarkIcon,
    PlusIcon,
    TrashIcon,
    ArrowUpIcon,
    ArrowDownIcon,
} from '@heroicons/react/24/outline';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';

interface QuizFormModalProps {
    quiz: Quiz | null;
    onClose: () => void;
    onSuccess: () => void;
}

const INPUT_TYPES = [
    'text',
    'number',
    'email',
    'date',
    'choice',
    'multipleChoice',
    'scale',
    'textarea',
    'tel',
    'url',
] as const;

export default function QuizFormModal({
    quiz,
    onClose,
    onSuccess,
}: QuizFormModalProps) {
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const [questions, setQuestions] = useState<QuizQuestion[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (quiz) {
            setTitle(quiz.title);
            setDescription(quiz.description || '');
            setQuestions(quiz.questions || []);
        } else {
            setTitle('');
            setDescription('');
            setQuestions([]);
        }
    }, [quiz]);

    const addQuestion = () => {
        const newQuestion: QuizQuestion = {
            label: '',
            inputType: 'text',
            mandatory: false,
            visible: true,
        };
        setQuestions([...questions, newQuestion]);
    };

    const removeQuestion = (index: number) => {
        setQuestions(questions.filter((_, i) => i !== index));
    };

    const moveQuestion = (index: number, direction: 'up' | 'down') => {
        if (
            (direction === 'up' && index === 0) ||
            (direction === 'down' && index === questions.length - 1)
        ) {
            return;
        }

        const newQuestions = [...questions];
        const targetIndex = direction === 'up' ? index - 1 : index + 1;
        [newQuestions[index], newQuestions[targetIndex]] = [
            newQuestions[targetIndex],
            newQuestions[index],
        ];
        setQuestions(newQuestions);
    };

    const updateQuestion = (index: number, updates: Partial<QuizQuestion>) => {
        const newQuestions = [...questions];
        newQuestions[index] = { ...newQuestions[index], ...updates };
        setQuestions(newQuestions);
    };

    const addChoice = (questionIndex: number) => {
        const question = questions[questionIndex];
        const choices = question.choices || [];
        updateQuestion(questionIndex, {
            choices: [...choices, ''],
        });
    };

    const updateChoice = (
        questionIndex: number,
        choiceIndex: number,
        value: string
    ) => {
        const question = questions[questionIndex];
        const choices = [...(question.choices || [])];
        choices[choiceIndex] = value;
        updateQuestion(questionIndex, { choices });
    };

    const removeChoice = (questionIndex: number, choiceIndex: number) => {
        const question = questions[questionIndex];
        const choices = (question.choices || []).filter(
            (_, i) => i !== choiceIndex
        );
        updateQuestion(questionIndex, { choices });
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);

        if (!title.trim()) {
            setError('Quiz title is required');
            return;
        }

        if (questions.length === 0) {
            setError('Quiz must have at least one question');
            return;
        }

        // Validate questions
        for (let i = 0; i < questions.length; i++) {
            const q = questions[i];
            if (!q.label.trim()) {
                setError(`Question ${i + 1} must have a label`);
                return;
            }

            if (
                (q.inputType === 'choice' ||
                    q.inputType === 'multipleChoice') &&
                (!q.choices || q.choices.length === 0)
            ) {
                setError(
                    `Question ${i + 1} of type ${
                        q.inputType
                    } must have at least one choice`
                );
                return;
            }

            if (
                (q.inputType === 'number' || q.inputType === 'scale') &&
                q.minValue !== undefined &&
                q.maxValue !== undefined &&
                q.minValue > q.maxValue
            ) {
                setError(`Question ${i + 1} has invalid min/max values`);
                return;
            }
        }

        try {
            setLoading(true);

            // Prepare questions with order and preserve IDs for updates
            const questionsWithOrder = questions.map((q, index) => {
                const questionData: any = {
                    label: q.label,
                    inputType: q.inputType,
                    mandatory: q.mandatory,
                    visible: q.visible,
                    minValue: q.minValue,
                    maxValue: q.maxValue,
                    choices: q.choices,
                    placeholder: q.placeholder,
                    hint: q.hint,
                };

                // Include ID if present (for updates)
                if (q.id) {
                    questionData.id = q.id;
                }

                return questionData;
            });

            if (quiz) {
                // Update existing quiz
                const response = await fetchWithAuth(
                    `/api/admin/quizzes/${quiz.id}`,
                    {
                        method: 'PUT',
                        headers: {
                            'Content-Type': 'application/json',
                        },
                        body: JSON.stringify({
                            title: title.trim(),
                            description: description.trim() || undefined,
                            questions: questionsWithOrder,
                        }),
                    }
                );

                if (!response.ok) {
                    const errorData = await response.json();
                    throw new Error(
                        errorData.error?.message || 'Failed to update quiz'
                    );
                }
            } else {
                // Create new quiz
                const response = await fetchWithAuth('/api/admin/quizzes', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        title: title.trim(),
                        description: description.trim() || undefined,
                        questions: questionsWithOrder,
                    }),
                });

                if (!response.ok) {
                    const errorData = await response.json();
                    throw new Error(
                        errorData.error?.message || 'Failed to create quiz'
                    );
                }
            }

            onSuccess();
        } catch (err) {
            setError(
                err instanceof Error ? err.message : 'Failed to save quiz'
            );
            console.error('Error saving quiz:', err);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="fixed inset-0 z-50 overflow-y-auto">
            <div className="flex min-h-screen items-center justify-center p-4">
                <div
                    className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
                    onClick={onClose}
                ></div>

                <div className="relative bg-white rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-hidden flex flex-col">
                    {/* Header */}
                    <div className="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
                        <h2 className="text-xl font-semibold text-gray-900">
                            {quiz ? 'Edit Quiz' : 'Create New Quiz'}
                        </h2>
                        <button
                            onClick={onClose}
                            className="text-gray-400 hover:text-gray-500"
                        >
                            <XMarkIcon className="h-6 w-6" />
                        </button>
                    </div>

                    {/* Content */}
                    <form
                        onSubmit={handleSubmit}
                        className="flex-1 overflow-y-auto"
                    >
                        <div className="p-6 space-y-6">
                            {error && (
                                <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
                                    {error}
                                </div>
                            )}

                            {/* Quiz Basic Info */}
                            <div className="space-y-4">
                                <div>
                                    <label className="block text-sm font-medium text-gray-700">
                                        Title{' '}
                                        <span className="text-red-500">*</span>
                                    </label>
                                    <input
                                        type="text"
                                        value={title}
                                        onChange={(e) =>
                                            setTitle(e.target.value)
                                        }
                                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                        required
                                    />
                                </div>

                                <div>
                                    <label className="block text-sm font-medium text-gray-700">
                                        Description
                                    </label>
                                    <textarea
                                        value={description}
                                        onChange={(e) =>
                                            setDescription(e.target.value)
                                        }
                                        rows={3}
                                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                    />
                                </div>
                            </div>

                            {/* Questions */}
                            <div className="space-y-4">
                                <div className="flex items-center justify-between">
                                    <h3 className="text-lg font-medium text-gray-900">
                                        Questions ({questions.length})
                                    </h3>
                                    <button
                                        type="button"
                                        onClick={addQuestion}
                                        className="inline-flex items-center px-3 py-2 border border-transparent text-sm leading-4 font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
                                    >
                                        <PlusIcon className="h-4 w-4 mr-1" />
                                        Add Question
                                    </button>
                                </div>

                                {questions.length === 0 ? (
                                    <div className="text-center py-8 text-gray-500">
                                        <p>
                                            No questions yet. Click "Add
                                            Question" to get started.
                                        </p>
                                    </div>
                                ) : (
                                    <div className="space-y-4">
                                        {questions.map((question, index) => (
                                            <div
                                                key={index}
                                                className="border border-gray-200 rounded-lg p-4 space-y-4"
                                            >
                                                <div className="flex items-center justify-between">
                                                    <h4 className="text-sm font-medium text-gray-700">
                                                        Question {index + 1}
                                                    </h4>
                                                    <div className="flex items-center space-x-2">
                                                        <button
                                                            type="button"
                                                            onClick={() =>
                                                                moveQuestion(
                                                                    index,
                                                                    'up'
                                                                )
                                                            }
                                                            disabled={
                                                                index === 0
                                                            }
                                                            className="text-gray-400 hover:text-gray-600 disabled:opacity-50"
                                                        >
                                                            <ArrowUpIcon className="h-4 w-4" />
                                                        </button>
                                                        <button
                                                            type="button"
                                                            onClick={() =>
                                                                moveQuestion(
                                                                    index,
                                                                    'down'
                                                                )
                                                            }
                                                            disabled={
                                                                index ===
                                                                questions.length -
                                                                    1
                                                            }
                                                            className="text-gray-400 hover:text-gray-600 disabled:opacity-50"
                                                        >
                                                            <ArrowDownIcon className="h-4 w-4" />
                                                        </button>
                                                        <button
                                                            type="button"
                                                            onClick={() =>
                                                                removeQuestion(
                                                                    index
                                                                )
                                                            }
                                                            className="text-red-600 hover:text-red-800"
                                                        >
                                                            <TrashIcon className="h-4 w-4" />
                                                        </button>
                                                    </div>
                                                </div>

                                                <div>
                                                    <label className="block text-sm font-medium text-gray-700">
                                                        Label{' '}
                                                        <span className="text-red-500">
                                                            *
                                                        </span>
                                                    </label>
                                                    <input
                                                        type="text"
                                                        value={question.label}
                                                        onChange={(e) =>
                                                            updateQuestion(
                                                                index,
                                                                {
                                                                    label: e
                                                                        .target
                                                                        .value,
                                                                }
                                                            )
                                                        }
                                                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                                        required
                                                    />
                                                </div>

                                                <div className="grid grid-cols-2 gap-4">
                                                    <div>
                                                        <label className="block text-sm font-medium text-gray-700">
                                                            Input Type{' '}
                                                            <span className="text-red-500">
                                                                *
                                                            </span>
                                                        </label>
                                                        <select
                                                            value={
                                                                question.inputType
                                                            }
                                                            onChange={(e) =>
                                                                updateQuestion(
                                                                    index,
                                                                    {
                                                                        inputType:
                                                                            e
                                                                                .target
                                                                                .value as QuizQuestion['inputType'],
                                                                        // Clear choices if not choice/multipleChoice
                                                                        choices:
                                                                            e
                                                                                .target
                                                                                .value ===
                                                                                'choice' ||
                                                                            e
                                                                                .target
                                                                                .value ===
                                                                                'multipleChoice'
                                                                                ? question.choices ||
                                                                                  []
                                                                                : undefined,
                                                                    }
                                                                )
                                                            }
                                                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                                        >
                                                            {INPUT_TYPES.map(
                                                                (type) => (
                                                                    <option
                                                                        key={
                                                                            type
                                                                        }
                                                                        value={
                                                                            type
                                                                        }
                                                                    >
                                                                        {type}
                                                                    </option>
                                                                )
                                                            )}
                                                        </select>
                                                    </div>

                                                    <div className="flex items-center space-x-4">
                                                        <label className="flex items-center">
                                                            <input
                                                                type="checkbox"
                                                                checked={
                                                                    question.mandatory
                                                                }
                                                                onChange={(e) =>
                                                                    updateQuestion(
                                                                        index,
                                                                        {
                                                                            mandatory:
                                                                                e
                                                                                    .target
                                                                                    .checked,
                                                                        }
                                                                    )
                                                                }
                                                                className="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                                                            />
                                                            <span className="ml-2 text-sm text-gray-700">
                                                                Mandatory
                                                            </span>
                                                        </label>
                                                        <label className="flex items-center">
                                                            <input
                                                                type="checkbox"
                                                                checked={
                                                                    question.visible
                                                                }
                                                                onChange={(e) =>
                                                                    updateQuestion(
                                                                        index,
                                                                        {
                                                                            visible:
                                                                                e
                                                                                    .target
                                                                                    .checked,
                                                                        }
                                                                    )
                                                                }
                                                                className="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                                                            />
                                                            <span className="ml-2 text-sm text-gray-700">
                                                                Visible
                                                            </span>
                                                        </label>
                                                    </div>
                                                </div>

                                                {(question.inputType ===
                                                    'number' ||
                                                    question.inputType ===
                                                        'scale') && (
                                                    <div className="grid grid-cols-2 gap-4">
                                                        <div>
                                                            <label className="block text-sm font-medium text-gray-700">
                                                                Min Value
                                                            </label>
                                                            <input
                                                                type="number"
                                                                value={
                                                                    question.minValue ||
                                                                    ''
                                                                }
                                                                onChange={(e) =>
                                                                    updateQuestion(
                                                                        index,
                                                                        {
                                                                            minValue:
                                                                                e
                                                                                    .target
                                                                                    .value
                                                                                    ? parseFloat(
                                                                                          e
                                                                                              .target
                                                                                              .value
                                                                                      )
                                                                                    : undefined,
                                                                        }
                                                                    )
                                                                }
                                                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                                            />
                                                        </div>
                                                        <div>
                                                            <label className="block text-sm font-medium text-gray-700">
                                                                Max Value
                                                            </label>
                                                            <input
                                                                type="number"
                                                                value={
                                                                    question.maxValue ||
                                                                    ''
                                                                }
                                                                onChange={(e) =>
                                                                    updateQuestion(
                                                                        index,
                                                                        {
                                                                            maxValue:
                                                                                e
                                                                                    .target
                                                                                    .value
                                                                                    ? parseFloat(
                                                                                          e
                                                                                              .target
                                                                                              .value
                                                                                      )
                                                                                    : undefined,
                                                                        }
                                                                    )
                                                                }
                                                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                                            />
                                                        </div>
                                                    </div>
                                                )}

                                                {(question.inputType ===
                                                    'choice' ||
                                                    question.inputType ===
                                                        'multipleChoice') && (
                                                    <div>
                                                        <div className="flex items-center justify-between mb-2">
                                                            <label className="block text-sm font-medium text-gray-700">
                                                                Choices{' '}
                                                                <span className="text-red-500">
                                                                    *
                                                                </span>
                                                            </label>
                                                            <button
                                                                type="button"
                                                                onClick={() =>
                                                                    addChoice(
                                                                        index
                                                                    )
                                                                }
                                                                className="text-sm text-indigo-600 hover:text-indigo-800"
                                                            >
                                                                <PlusIcon className="h-4 w-4 inline mr-1" />
                                                                Add Choice
                                                            </button>
                                                        </div>
                                                        <div className="space-y-2">
                                                            {(
                                                                question.choices ||
                                                                []
                                                            ).map(
                                                                (
                                                                    choice,
                                                                    choiceIndex
                                                                ) => (
                                                                    <div
                                                                        key={
                                                                            choiceIndex
                                                                        }
                                                                        className="flex items-center space-x-2"
                                                                    >
                                                                        <input
                                                                            type="text"
                                                                            value={
                                                                                choice
                                                                            }
                                                                            onChange={(
                                                                                e
                                                                            ) =>
                                                                                updateChoice(
                                                                                    index,
                                                                                    choiceIndex,
                                                                                    e
                                                                                        .target
                                                                                        .value
                                                                                )
                                                                            }
                                                                            className="flex-1 rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                                                            placeholder="Choice text"
                                                                        />
                                                                        <button
                                                                            type="button"
                                                                            onClick={() =>
                                                                                removeChoice(
                                                                                    index,
                                                                                    choiceIndex
                                                                                )
                                                                            }
                                                                            className="text-red-600 hover:text-red-800"
                                                                        >
                                                                            <TrashIcon className="h-4 w-4" />
                                                                        </button>
                                                                    </div>
                                                                )
                                                            )}
                                                        </div>
                                                    </div>
                                                )}

                                                <div className="grid grid-cols-2 gap-4">
                                                    <div>
                                                        <label className="block text-sm font-medium text-gray-700">
                                                            Placeholder
                                                        </label>
                                                        <input
                                                            type="text"
                                                            value={
                                                                question.placeholder ||
                                                                ''
                                                            }
                                                            onChange={(e) =>
                                                                updateQuestion(
                                                                    index,
                                                                    {
                                                                        placeholder:
                                                                            e
                                                                                .target
                                                                                .value ||
                                                                            undefined,
                                                                    }
                                                                )
                                                            }
                                                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                                        />
                                                    </div>
                                                    <div>
                                                        <label className="block text-sm font-medium text-gray-700">
                                                            Hint
                                                        </label>
                                                        <input
                                                            type="text"
                                                            value={
                                                                question.hint ||
                                                                ''
                                                            }
                                                            onChange={(e) =>
                                                                updateQuestion(
                                                                    index,
                                                                    {
                                                                        hint:
                                                                            e
                                                                                .target
                                                                                .value ||
                                                                            undefined,
                                                                    }
                                                                )
                                                            }
                                                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                                        />
                                                    </div>
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </div>
                        </div>

                        {/* Footer */}
                        <div className="px-6 py-4 border-t border-gray-200 flex items-center justify-end space-x-3">
                            <button
                                type="button"
                                onClick={onClose}
                                className="px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50"
                            >
                                Cancel
                            </button>
                            <button
                                type="submit"
                                disabled={loading}
                                className="px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50"
                            >
                                {loading
                                    ? 'Saving...'
                                    : quiz
                                    ? 'Update Quiz'
                                    : 'Create Quiz'}
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    );
}
