export interface Auth0Resource {
    path: string;
    label: string;
}

export type UserRole = 'admin' | 'coach' | 'user';

export interface User {
    id: string;
    email: string;
    fullName: string;
    firstName?: string;
    favoriteColor: string;
    doB: string; // ISO date string
    isOnboarded: boolean;
    roles: UserRole[];
}

export interface OnboardingData {
    email: string;
    fullName: string;
    doB: string;
    favoriteColor: string;
    role: UserRole;
}

export interface CoachOnboardingData extends OnboardingData {
    address: string;
    experience: string;
}

export interface UserOnboardingData extends OnboardingData {
    preferredPronoun: string;
    neurodivergentDetails: string;
}

export interface ChatMessage {
    role: 'user' | 'assistant';
    content: string;
    createdAt?: string;
}

export interface Conversation {
    id: string;
    userId: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    messages?: ChatMessage[];
}

export interface ChatRequest {
    conversationId?: string;
    content: string;
}

export interface ChatStreamChunk {
    type: 'text';
    value: string;
}

export interface UploadResult {
    status: 'uploaded' | 'error';
    filename: string;
    fileSize?: number;
    message?: string;
    location?: string;
}

export interface Resource {
    id: string;
    fileName: string;
    location: string;
    sizeInBytes: number;
    createdAt: string;
    updatedAt: string;
}
