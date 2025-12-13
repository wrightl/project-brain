import { UserService } from '@/_services/user-service';
import MessageInterface from '../../../../../_components/message-interface';
import { redirect, notFound } from 'next/navigation';
import { isValidGuid } from '@/_lib/utils';

export const dynamic = 'force-dynamic';
export const dynamicParams = true;

export default async function UserMessagePage({
    params,
}: {
    params: Promise<{ id: string }>;
}) {
    const user = await UserService.getCurrentUser();
    if (!user) {
        redirect('/auth/login?returnTo=/app/user/messages');
    }

    const { id } = await params;

    // Validate connectionId is a valid GUID
    if (!isValidGuid(id)) {
        notFound(); // Return 404 for invalid GUIDs
    }

    return <MessageInterface connectionId={id} />;
}
