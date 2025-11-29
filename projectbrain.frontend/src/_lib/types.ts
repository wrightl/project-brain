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
    isOnboarded: boolean;
    roles: UserRole[];
    lastActivityAt?: string;
    // Profile fields
    doB?: string;
    preferredPronoun?: string;
    neurodiverseTraits?: string[];
    preferences?: string;
    // Address fields
    streetAddress?: string;
    addressLine2?: string;
    city?: string;
    stateProvince?: string;
    postalCode?: string;
    country?: string;
}

export interface Coach extends User {
    qualifications: string[];
    specialisms: string[];
    ageGroups: string[];
}

export interface OnboardingData {
    email: string;
    fullName: string;
    // role: UserRole;
    // Address fields
    streetAddress?: string;
    addressLine2?: string;
    city?: string;
    stateProvince?: string;
    postalCode?: string;
    country?: string;
}

export interface CoachOnboardingData extends OnboardingData {
    qualifications?: string[];
    specialisms?: string[];
    ageGroups?: string[];
}

export interface UserOnboardingData extends OnboardingData {
    doB: string;
    preferredPronoun: string;
    neurodiverseTraits?: string[];
    preferences?: string;
}

export interface Citation {
    id: string;
    index: number;
    sourceFile: string;
    sourcePage?: string;
    storageUrl: string;
    isShared: boolean;
}

export interface ChatMessage {
    role: 'user' | 'assistant';
    content: string;
    createdAt?: string;
    citations?: Citation[];
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

export interface ReindexResult {
    status: 'success' | 'error';
    filesReindexed: number;
}
