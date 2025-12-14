import { RoleGuard } from '@/_components/auth/role-guard';
import QuizResponseView from './_components/quiz-response-view';

export default async function QuizResponsePage({
    params,
}: {
    params: Promise<{ responseId: string }>;
}) {
    const { responseId } = await params;

    return (
        <RoleGuard allowedRoles={['user']}>
            <div className="space-y-6">
                <QuizResponseView responseId={responseId} />
            </div>
        </RoleGuard>
    );
}

