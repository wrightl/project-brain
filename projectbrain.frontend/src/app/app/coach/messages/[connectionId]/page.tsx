import { UserService } from '@/_services/user-service';
import MessageInterface from '../../../../../_components/message-interface';
import { redirect, notFound } from 'next/navigation';
import { isValidGuid } from '@/_lib/utils';

export const dynamic = 'force-dynamic';
export const dynamicParams = true;

export default async function CoachMessagePage({
    params,
}: {
    params: Promise<{ connectionId: string }>;
}) {
    const user = await UserService.getCurrentUser();
    if (!user) {
        redirect('/auth/login?returnTo=/app/coach/messages');
    }

    const { connectionId } = await params;

    // Validate connectionId is a valid GUID
    if (!isValidGuid(connectionId)) {
        notFound(); // Return 404 for invalid GUIDs
    }

    return <MessageInterface connectionId={connectionId} />;
}
