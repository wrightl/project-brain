import { NextRequest, NextResponse } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { User } from '@/_lib/types';
import { UserService } from '@/_services/user-service';
import { BackendApiError } from '@/_lib/backend-api';

export const GET = createApiRoute<User>(async (req: NextRequest) => {
    const pathname = req.nextUrl.pathname;
    const id = pathname.split('/').pop();

    if (!id) {
        throw new BackendApiError(400, 'User ID is required');
    }

    const user = await UserService.getUserById(id);

    if (!user) {
        throw new BackendApiError(404, 'User not found');
    }

    return user;
});

export const PUT = createApiRoute<User>(async (req: NextRequest) => {
    const pathname = req.nextUrl.pathname;
    const id = pathname.split('/').pop();

    if (!id) {
        throw new BackendApiError(400, 'User ID is required');
    }

    const body = await req.json();
    const updatedUser = await UserService.updateUser(id, body);

    return updatedUser;
});

export const DELETE = createApiRoute(async (req: NextRequest) => {
    const pathname = req.nextUrl.pathname;
    const id = pathname.split('/').pop();

    if (!id) {
        throw new BackendApiError(400, 'User ID is required');
    }

    await UserService.deleteUser(id);

    return NextResponse.json({ success: true });
});

