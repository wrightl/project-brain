import { callBackendApi } from '@/_lib/backend-api';
import { PagedResponse } from '@/_lib/types';

export interface QuizQuestion {
    id?: string;
    label: string;
    inputType:
        | 'text'
        | 'number'
        | 'email'
        | 'date'
        | 'choice'
        | 'multipleChoice'
        | 'scale'
        | 'textarea'
        | 'tel'
        | 'url';
    mandatory: boolean;
    visible: boolean;
    minValue?: number;
    maxValue?: number;
    choices?: string[];
    placeholder?: string;
    hint?: string;
}

export interface Quiz {
    id: string;
    title: string;
    description?: string;
    questions?: QuizQuestion[];
    createdAt: string;
    updatedAt: string;
}

export interface QuizResponse {
    id: string;
    quizId: string;
    quizTitle?: string;
    userId: string;
    answers?: Record<string, unknown>;
    score?: number;
    completedAt: string;
    createdAt: string;
}

export class QuizService {
    /**
     * Get all quizzes
     */
    static async getAllQuizzes(options?: {
        page?: number;
        pageSize?: number;
    }): Promise<PagedResponse<Quiz>> {
        const params = new URLSearchParams();
        if (options?.page) {
            params.append('page', options.page.toString());
        }
        if (options?.pageSize) {
            params.append('pageSize', options.pageSize.toString());
        }
        const queryParam = params.toString() ? `?${params.toString()}` : '';
        const response = await callBackendApi(`/quizes${queryParam}`);
        if (!response.ok) {
            throw new Error('Failed to fetch quizzes');
        }
        return response.json();
    }

    /**
     * Get quiz by ID with questions
     */
    static async getQuizById(id: string): Promise<Quiz> {
        const response = await callBackendApi(`/quizes/${id}`);
        if (!response.ok) {
            throw new Error('Failed to fetch quiz');
        }
        return response.json();
    }

    /**
     * Create a new quiz (admin only)
     */
    static async createQuiz(quiz: {
        title: string;
        description?: string;
        questions: QuizQuestion[];
    }): Promise<Quiz> {
        const response = await callBackendApi('/quizes', {
            method: 'POST',
            body: quiz,
        });
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(
                errorData.error?.message || 'Failed to create quiz'
            );
        }
        return response.json();
    }

    /**
     * Delete a quiz (admin only)
     */
    static async deleteQuiz(id: string): Promise<void> {
        const response = await callBackendApi(`/quizes/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(
                errorData.error?.message || 'Failed to delete quiz'
            );
        }
    }

    /**
     * Get a single quiz response by ID
     */
    static async getQuizResponseById(responseId: string): Promise<QuizResponse> {
        const response = await callBackendApi(`/quizes/responses/${responseId}`);
        if (!response.ok) {
            throw new Error('Failed to fetch quiz response');
        }
        return response.json();
    }

    /**
     * Get all quiz responses for the current user
     */
    static async getQuizResponses(options?: {
        page?: number;
        pageSize?: number;
    }): Promise<PagedResponse<QuizResponse>> {
        const params = new URLSearchParams();
        if (options?.page) {
            params.append('page', options.page.toString());
        }
        if (options?.pageSize) {
            params.append('pageSize', options.pageSize.toString());
        }
        const queryParam = params.toString() ? `?${params.toString()}` : '';
        const response = await callBackendApi(`/quizes/responses${queryParam}`);
        if (!response.ok) {
            throw new Error('Failed to fetch quiz responses');
        }
        return response.json();
    }

    /**
     * Get quiz response count for the current user
     */
    static async getQuizResponseCount(): Promise<{ count: number }> {
        const response = await callBackendApi('/quizes/responses/count');
        if (!response.ok) {
            throw new Error('Failed to fetch quiz response count');
        }
        return response.json();
    }

    /**
     * Delete a quiz response
     */
    static async deleteQuizResponse(responseId: string): Promise<void> {
        const response = await callBackendApi(`/quizes/responses/${responseId}`, {
            method: 'DELETE',
        });
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(
                errorData.error?.message || 'Failed to delete quiz response'
            );
        }
    }

    /**
     * Submit a quiz response
     */
    static async submitQuizResponse(
        quizId: string,
        answers: Record<string, unknown>,
        completedAt?: string
    ): Promise<QuizResponse> {
        const response = await callBackendApi(`/quizes/${quizId}/responses`, {
            method: 'POST',
            body: {
                answers,
                completedAt,
            },
        });
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(
                errorData.error?.message || 'Failed to submit quiz response'
            );
        }
        return response.json();
    }

    /**
     * Get quiz insights for the current user
     */
    static async getQuizInsights(): Promise<{
        summary: string;
        keyInsights: string[];
        lastUpdated: string;
    }> {
        const response = await callBackendApi('/quizes/insights');
        if (!response.ok) {
            throw new Error('Failed to fetch quiz insights');
        }
        return response.json();
    }
}
