import { createApiRoute } from '@/_lib/api-route-handler';
import { CoachService, ClientWithConnectionStatus } from '@/_services/coach-service';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<ClientWithConnectionStatus[]>(
    async (req: NextRequest) => {
        const clients = await CoachService.getConnectedClients();
        return clients;
    }
);

