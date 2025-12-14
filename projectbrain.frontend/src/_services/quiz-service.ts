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
    userId: string;
    answers: Record<string, unknown>;
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
}
