'use client';

import { Coach } from '@/_lib/types';
import { ThemeSelector } from '@/_components/theme-selector';

interface PreferencesSectionProps {
    coach: Coach;
}

export default function PreferencesSection({
    coach: _coach,
}: PreferencesSectionProps) {
    return <ThemeSelector />;
}
