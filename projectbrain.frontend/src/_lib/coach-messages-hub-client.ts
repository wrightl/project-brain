const HUB_PATH = '/hubs/coach-messages';

type SignalRModule = typeof import('@microsoft/signalr');
type HubConnection = import('@microsoft/signalr').HubConnection;
type HubConnectionState = import('@microsoft/signalr').HubConnectionState;

type HubEventHandler = (...args: unknown[]) => void;

let signalRModule: SignalRModule | null = null;
let hubConnection: HubConnection | null = null;
let startPromise: Promise<void> | null = null;
let subscriberCount = 0;

const eventHandlers = new Map<string, Set<HubEventHandler>>();

async function getSignalR(): Promise<SignalRModule> {
    if (!signalRModule) {
        signalRModule = await import('@microsoft/signalr');
    }
    return signalRModule;
}

function getApiUrl(): string {
    return process.env.NEXT_PUBLIC_API_SERVER_URL || 'https://localhost:7585';
}

async function fetchHubToken(): Promise<string> {
    const response = await fetch('/api/signalr/hub-token');
    if (!response.ok) {
        throw new Error('Failed to get SignalR hub token');
    }
    const data = (await response.json()) as { token: string };
    return data.token;
}

function attachHubListeners(connection: HubConnection): void {
    for (const [eventName, handlers] of eventHandlers) {
        for (const handler of handlers) {
            connection.on(eventName, handler);
        }
    }
}

function detachHubListeners(connection: HubConnection): void {
    for (const [eventName, handlers] of eventHandlers) {
        for (const handler of handlers) {
            connection.off(eventName, handler);
        }
    }
}

export async function acquireCoachMessagesHub(): Promise<HubConnection> {
    subscriberCount++;

    if (!hubConnection) {
        const signalR = await getSignalR();
        hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${getApiUrl()}${HUB_PATH}`, {
                accessTokenFactory: fetchHubToken,
            })
            .withAutomaticReconnect()
            .build();

        attachHubListeners(hubConnection);

        startPromise = hubConnection.start().catch((error) => {
            hubConnection = null;
            startPromise = null;
            subscriberCount = 0;
            throw error;
        });
    }

    await startPromise;
    return hubConnection!;
}

export function releaseCoachMessagesHub(): void {
    subscriberCount = Math.max(0, subscriberCount - 1);

    if (subscriberCount === 0 && hubConnection) {
        const connection = hubConnection;
        detachHubListeners(connection);
        hubConnection = null;
        startPromise = null;
        connection.stop().catch(() => {});
    }
}

export function subscribeCoachMessagesHubEvent(
    eventName: string,
    handler: HubEventHandler,
): () => void {
    if (!eventHandlers.has(eventName)) {
        eventHandlers.set(eventName, new Set());
    }

    const handlers = eventHandlers.get(eventName)!;
    handlers.add(handler);

    if (hubConnection) {
        hubConnection.on(eventName, handler);
    }

    return () => {
        handlers.delete(handler);
        hubConnection?.off(eventName, handler);
    };
}

export async function invokeCoachMessagesHub(
    methodName: string,
    ...args: unknown[]
): Promise<unknown> {
    const connection = await acquireCoachMessagesHub();
    return connection.invoke(methodName, ...args);
}

export function getCoachMessagesHubState(): HubConnectionState | null {
    return hubConnection?.state ?? null;
}
