const BASE = import.meta.env.VITE_API_URL || '/api';

export async function api(path: string, options: RequestInit = {}) {
  const token = localStorage.getItem('token');
  const headers = new Headers(options.headers);
  if (!headers.has('Content-Type') && options.body) headers.set('Content-Type', 'application/json');
  if (token) headers.set('Authorization', `Bearer ${token}`);

  const res = await fetch(BASE + path, { ...options, headers });
  if (res.status === 401) {
    localStorage.removeItem('token');
    if (window.location.pathname !== '/login') window.location.href = '/login';
    throw new Error('Unauthorized');
  }
  if (!res.ok) {
    const payload = await res.json().catch(() => ({} as { message?: string }));
    throw new Error(payload.message || 'Something went wrong');
  }
  if (res.status === 204) return null;
  const text = await res.text();
  return text ? JSON.parse(text) : null;
}
