export interface Auth0Resource {
    path: string;
    label: string;
}

export type UserRole = 'admin' | 'coach' | 'user';

export interface BaseUser {
    id: string; // TODO: Might need to remove this
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

export interface User extends BaseUser {
    userProfileId: string;
}

export interface Coach extends BaseUser {
    coachProfileId: string;
    qualifications: string[];
    specialisms: string[];
    ageGroups: string[];
    availabilityStatus?: 'Available' | 'Busy' | 'Away' | 'Offline';
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

// Subscription types
export interface Subscription {
    id?: string;
    tier: string;
    status: string;
    trialEndsAt?: string;
    currentPeriodStart?: string;
    currentPeriodEnd?: string;
    canceledAt?: string;
    userType: string;
}

export interface Usage {
    aiQueries: {
        daily: number;
        monthly: number;
    };
    coachMessages: {
        monthly: number;
    };
    clientMessages: {
        monthly: number;
    };
    fileStorage: {
        bytes: number;
        megabytes: number;
    };
    researchReports: {
        monthly: number;
    };
}

// Voice note types
export interface VoiceNote {
    id: string;
    fileName: string;
    audioUrl: string;
    duration: number | null;
    fileSize: number | null;
    description: string | null;
    createdAt: string;
    updatedAt: string;
}

export interface PagedResponse<T> {
    items: T[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasPreviousPage: boolean;
    hasNextPage: boolean;
}

// Connection types
export interface Connection {
    id: string;
    userId: string;
    coachId: string;
    status: 'pending' | 'accepted' | 'cancelled' | 'rejected';
    userName?: string;
    coachName?: string;
    requestedAt: string;
    respondedAt?: string;
}

// Coach search types
export interface CoachSearchParams {
    city?: string;
    stateProvince?: string;
    country?: string;
    ageGroups?: string[];
    specialisms?: string[];
}

export interface ClientWithConnectionStatus {
    user: User;
    connectionStatus: 'pending' | 'accepted';
    requestedAt: string;
    requestedBy: 'user' | 'coach';
    message?: string;
}
