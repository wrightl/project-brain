import { createApiRoute } from '@/_lib/api-route-handler';
import { ConnectionService, Connection } from '@/_services/connection-service';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<Connection[]>(async (req: NextRequest) => {
    const connections = await ConnectionService.getConnections();
    return connections;
});

