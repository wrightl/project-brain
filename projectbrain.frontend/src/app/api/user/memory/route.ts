import { createApiRoute } from '@/_lib/api-route-handler';
import { UserMemoryService } from '@/_services/user-memory-service';
import { NextRequest } from 'next/server';
import { callBackendApi } from '@/_lib/backend-api';

export const GET = createApiRoute(async () => {
    return UserMemoryService.listMemories();
});
