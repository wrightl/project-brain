import { permanentRedirect } from 'next/navigation';

// The product overview now lives on the homepage; keep old links working.
export default function ProductPage() {
    permanentRedirect('/#product');
}
