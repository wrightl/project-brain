import { RoleGuard } from '@/_components/auth/role-guard';
import QuizResponsesList from './_components/quiz-responses-list';

export default async function QuizResponsesPage() {
    return (
        <RoleGuard allowedRoles={['user']}>
            <div className="space-y-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">
                        Quiz Responses
                    </h1>
                    <p className="mt-1 text-sm text-gray-600">
                        View and manage your quiz responses
                    </p>
                </div>

                <QuizResponsesList />
            </div>
        </RoleGuard>
    );
}

