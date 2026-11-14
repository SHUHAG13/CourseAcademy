import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import { ApiError, NETWORK_ERROR_MESSAGE, UNEXPECTED_ERROR_MESSAGE } from './api-error';
import { API_BASE_URL } from './api-mode';
import { ApiResponse, QueryParams } from './api.models';

/**
 * Thin wrapper over HttpClient for the Course Academy API. Calls resolve with the response's `data`
 * and reject with an `ApiError`, so feature services never deal with the envelope or HTTP errors.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  get<T>(path: string, params?: QueryParams): Promise<T> {
    return this.send<T>(this.http.get<ApiResponse<T>>(this.url(path), { params: toHttpParams(params) }));
  }

  post<T>(path: string, body?: unknown): Promise<T> {
    return this.send<T>(this.http.post<ApiResponse<T>>(this.url(path), body ?? null));
  }

  put<T>(path: string, body?: unknown): Promise<T> {
    return this.send<T>(this.http.put<ApiResponse<T>>(this.url(path), body ?? null));
  }

  patch<T>(path: string, body?: unknown): Promise<T> {
    return this.send<T>(this.http.patch<ApiResponse<T>>(this.url(path), body ?? null));
  }

  delete<T = null>(path: string): Promise<T> {
    return this.send<T>(this.http.delete<ApiResponse<T>>(this.url(path)));
  }

  private url(path: string): string {
    return `${this.baseUrl}/${path.replace(/^\/+/, '')}`;
  }

  private async send<T>(request: Observable<ApiResponse<T>>): Promise<T> {
    let response: ApiResponse<T>;
    try {
      response = await firstValueFrom(request);
    } catch (error) {
      throw toApiError(error);
    }
    if (!response?.success) {
      throw new ApiError(200, response?.message || UNEXPECTED_ERROR_MESSAGE, response?.errors ?? []);
    }
    return response.data as T;
  }
}

/** Builds query params, leaving out empty values and joining arrays with commas. */
export function toHttpParams(params: QueryParams | undefined): HttpParams {
  let result = new HttpParams();
  for (const [key, value] of Object.entries(params ?? {})) {
    if (value === null || value === undefined || value === '') continue;
    if (Array.isArray(value)) {
      if (value.length) result = result.set(key, value.join(','));
    } else {
      result = result.set(key, String(value));
    }
  }
  return result;
}

/** Converts any HttpClient failure into an ApiError, reading the API envelope when the server sent one. */
export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;
  if (!(error instanceof HttpErrorResponse)) return new ApiError(0, UNEXPECTED_ERROR_MESSAGE);
  if (error.status === 0) return new ApiError(0, NETWORK_ERROR_MESSAGE);

  const body = error.error as Partial<ApiResponse<unknown>> | null;
  const message = typeof body?.message === 'string' && body.message ? body.message : UNEXPECTED_ERROR_MESSAGE;
  return new ApiError(error.status, message, Array.isArray(body?.errors) ? body.errors : []);
}
