import { RoleGuard } from '@/_components/auth/role-guard';
import QuizWizard from './_components/quiz-wizard';

export default async function TakeQuizPage({
    params,
}: {
    params: Promise<{ quizId: string }>;
}) {
    const { quizId } = await params;

    return (
        <RoleGuard allowedRoles={['user']}>
            <div className="space-y-6">
                <QuizWizard quizId={quizId} />
            </div>
        </RoleGuard>
    );
}

