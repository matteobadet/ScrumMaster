const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5248';

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

async function request<TResponse>(path: string, init?: RequestInit): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
  });

  if (!response.ok) {
    const body = await response.text();
    throw new ApiError(response.status, body || response.statusText);
  }

  if (response.status === 204) {
    return undefined as TResponse;
  }

  return (await response.json()) as TResponse;
}

export const apiClient = {
  get: <TResponse>(path: string) => request<TResponse>(path, { method: 'GET' }),
  post: <TResponse>(path: string, body: unknown) =>
    request<TResponse>(path, { method: 'POST', body: JSON.stringify(body) }),
};

export { API_BASE_URL };
