import { redirect } from 'next/navigation';
import { UserService } from '@/_services/user-service';
import ConversationList from '@/_components/coach-messages/conversation-list';

export const dynamic = 'force-dynamic';

export default async function CoachMessagesPage() {
    const user = await UserService.getCurrentUser();
    if (!user) {
        redirect('/auth/login?returnTo=/app/coach/messages');
    }

    return (
        <div className="max-w-4xl mx-auto py-8 px-4">
            <h1 className="text-2xl font-bold text-gray-900 mb-6">Messages</h1>
            <ConversationList />
        </div>
    );
}

