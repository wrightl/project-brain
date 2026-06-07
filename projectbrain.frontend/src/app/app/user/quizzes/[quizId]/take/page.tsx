import { RoleGuard } from '@/_components/auth/role-guard';
import QuizWizard from './_components/quiz-wizard';

import { AppRoles } from '@/_lib/roles';
export default async function TakeQuizPage({
    params,
}: {
    params: Promise<{ quizId: string }>;
}) {
    const { quizId } = await params;

    return (
        <RoleGuard allowedRoles={[AppRoles.User]}>
            <div className="space-y-6">
                <QuizWizard quizId={quizId} />
            </div>
        </RoleGuard>
    );
}

