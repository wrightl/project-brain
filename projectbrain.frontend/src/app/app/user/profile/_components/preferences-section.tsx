'use client';

import { User } from '@/_lib/types';
import { ThemeSelector } from '@/_components/theme-selector';

interface PreferencesSectionProps {
    user: User;
}

export default function PreferencesSection({
    user: _user,
}: PreferencesSectionProps) {
    return <ThemeSelector />;
}
