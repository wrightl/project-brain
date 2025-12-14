import { RoleGuard } from '@/_components/auth/role-guard';
import QuizList from './_components/quiz-list';

export default async function QuizzesPage() {
    return (
        <RoleGuard allowedRoles={['user']}>
            <div className="space-y-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">
                        Quizzes
                    </h1>
                    <p className="mt-1 text-sm text-gray-600">
                        Select a quiz to take
                    </p>
                </div>

                <QuizList />
            </div>
        </RoleGuard>
    );
}

