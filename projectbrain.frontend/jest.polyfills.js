// Jest runs tests in jsdom; fetch globals (Request/Response/Headers) are not
// available there by default. MSW v2 expects these to exist.
import 'whatwg-fetch';

// Some dependencies (e.g. MSW interceptors) expect Web Crypto text encoders.
import { TextDecoder, TextEncoder } from 'util';

if (!globalThis.TextEncoder) globalThis.TextEncoder = TextEncoder;
if (!globalThis.TextDecoder) globalThis.TextDecoder = TextDecoder;

// Streams API (needed by MSW interceptors in jsdom).
import { ReadableStream, TransformStream, WritableStream } from 'stream/web';
if (!globalThis.ReadableStream) globalThis.ReadableStream = ReadableStream;
if (!globalThis.TransformStream) globalThis.TransformStream = TransformStream;
if (!globalThis.WritableStream) globalThis.WritableStream = WritableStream;

// MSW uses BroadcastChannel for internal messaging.
import { BroadcastChannel } from 'worker_threads';
if (!globalThis.BroadcastChannel) globalThis.BroadcastChannel = BroadcastChannel;

