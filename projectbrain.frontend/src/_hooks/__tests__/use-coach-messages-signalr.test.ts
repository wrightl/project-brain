import { act, renderHook, waitFor } from '@/_lib/test-utils';
import { useCoachMessagesSignalR } from '@/_hooks/use-coach-messages-signalr';
import {
    acquireCoachMessagesHub,
    getCoachMessagesHubState,
    invokeCoachMessagesHub,
    onCoachMessagesHubReconnected,
    releaseCoachMessagesHub,
    subscribeCoachMessagesHubEvent,
} from '@/_lib/coach-messages-hub-client';

jest.mock('@/_lib/coach-messages-hub-client', () => ({
    acquireCoachMessagesHub: jest.fn(),
    getCoachMessagesHubState: jest.fn(),
    invokeCoachMessagesHub: jest.fn(),
    onCoachMessagesHubReconnected: jest.fn(),
    releaseCoachMessagesHub: jest.fn(),
    subscribeCoachMessagesHubEvent: jest.fn(),
}));

describe('useCoachMessagesSignalR', () => {
    let reconnectHandler: (() => void | Promise<void>) | undefined;

    beforeEach(() => {
        reconnectHandler = undefined;
        jest.clearAllMocks();

        (acquireCoachMessagesHub as jest.Mock).mockResolvedValue({});
        (getCoachMessagesHubState as jest.Mock).mockReturnValue('Connected');
        (invokeCoachMessagesHub as jest.Mock).mockResolvedValue(undefined);
        (subscribeCoachMessagesHubEvent as jest.Mock).mockReturnValue(jest.fn());
        (onCoachMessagesHubReconnected as jest.Mock).mockImplementation(
            (handler: () => void | Promise<void>) => {
                reconnectHandler = handler;
                return jest.fn();
            },
        );
    });

    it('rejoins the conversation group after SignalR reconnects', async () => {
        const { unmount } = renderHook(() =>
            useCoachMessagesSignalR({
                connectionId: 'connection-123',
                onNewMessage: jest.fn(),
                onTypingIndicator: jest.fn(),
            }),
        );

        await waitFor(() => {
            expect(invokeCoachMessagesHub).toHaveBeenCalledWith(
                'JoinConversation',
                'connection-123',
            );
        });

        await act(async () => {
            await reconnectHandler?.();
        });

        expect(invokeCoachMessagesHub).toHaveBeenCalledWith(
            'JoinConversation',
            'connection-123',
        );
        expect(
            (invokeCoachMessagesHub as jest.Mock).mock.calls.filter(
                ([methodName]) => methodName === 'JoinConversation',
            ),
        ).toHaveLength(2);

        unmount();
        expect(releaseCoachMessagesHub).toHaveBeenCalled();
    });
});
