export type UserRole = 'admin' | 'coach' | 'user';

export interface User {
  id: string;
  email: string;
  fullName: string;
  firstName?: string;
  favoriteColor: string;
  doB: string; // ISO date string
  isOnboarded: boolean;
  role?: UserRole;
}

export interface OnboardingData {
  email: string;
  fullName: string;
  doB: string;
  favoriteColor: string;
}

export interface CoachOnboardingData extends OnboardingData {
  address: string;
  experience: string;
}

export interface UserOnboardingData extends OnboardingData {
  preferredPronoun: string;
  neurodivergentDetails: string;
}
