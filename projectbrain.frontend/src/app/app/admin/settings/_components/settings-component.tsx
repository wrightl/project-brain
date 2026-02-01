'use client';

import { useEffect, useState } from 'react';
import AISettingsSection from './ai-settings-section';
import SubscriptionSettingsSection from './subscription-settings-section';
import ReferralSettingsSection from './referral-settings-section';

export default function SettingsComponent() {
    return (
        <div className="space-y-6">
            <div>
                <h1 className="text-2xl font-bold text-gray-900">Settings</h1>
                <p className="mt-1 text-sm text-gray-600">
                    Manage application settings and configuration
                </p>
            </div>

            <SubscriptionSettingsSection />
            <ReferralSettingsSection />
            <AISettingsSection />
        </div>
    );
}
