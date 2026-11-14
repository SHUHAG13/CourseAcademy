import { HttpClient, HttpContext, HttpContextToken } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { localStore, sessionStore } from '../data/browser-storage';
import { API_BASE_URL } from './api-mode';
import { ApiResponse } from './api.models';
import { TokensDto } from './auth.models';

/** Marks a request the auth interceptor must leave alone (no token, no refresh). */
export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);

const STORAGE_KEY = 'ca-auth';
/** Refresh this long before the access token expires, so requests don't fail on the boundary. */
const EXPIRY_MARGIN_MS = 30_000;

interface StoredTokens extends TokensDto {
  remember: boolean;
}

/**
 * Holds the API tokens and renews them. "Remember me" keeps them in localStorage, otherwise in
 * sessionStorage, which the browser clears when it closes. Has no dependency on AuthService, so the
 * HTTP interceptor can use it without a circular dependency.
 */
@Injectable({ providedIn: 'root' })
export class AuthSession {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);
  private refreshing: Promise<boolean> | null = null;

  /** Increases each time the session ends without the user logging out (refresh failed). */
  readonly expired = signal(0);
  /** Increases when another tab signs in or out. */
  readonly changedElsewhere = signal(0);

  constructor() {
    if (typeof window !== 'undefined') {
      window.addEventListener('storage', (e) => {
        if (e.key === STORAGE_KEY || e.key === null) this.changedElsewhere.update((n) => n + 1);
      });
    }
  }

  get hasSession(): boolean {
    return this.read() !== null;
  }

  /** The access token, or null. */
  get accessToken(): string | null {
    return this.read()?.accessToken ?? null;
  }

  /** True when the access token is missing or about to expire. */
  get accessTokenExpired(): boolean {
    const tokens = this.read();
    return !tokens || Date.parse(tokens.accessTokenExpiresAt) - EXPIRY_MARGIN_MS <= Date.now();
  }

  get refreshToken(): string | null {
    return this.read()?.refreshToken ?? null;
  }

  save(tokens: TokensDto, remember: boolean): void {
    this.clear();
    const stored: StoredTokens = {
      accessToken: tokens.accessToken,
      accessTokenExpiresAt: tokens.accessTokenExpiresAt,
      refreshToken: tokens.refreshToken,
      remember,
    };
    try {
      (remember ? localStore : sessionStore).setItem(STORAGE_KEY, JSON.stringify(stored));
    } catch {
      // Storage full or blocked: the session works until the page closes.
    }
  }

  clear(): void {
    localStore.removeItem(STORAGE_KEY);
    sessionStore.removeItem(STORAGE_KEY);
  }

  /**
   * Swaps the refresh token for new tokens. Concurrent callers share one request, because a
   * refresh token works only once. Resolves false (and ends the session) when it is refused.
   */
  refresh(): Promise<boolean> {
    this.refreshing ??= this.doRefresh().finally(() => (this.refreshing = null));
    return this.refreshing;
  }

  private async doRefresh(): Promise<boolean> {
    const current = this.read();
    if (!current) return false;
    try {
      const response = await firstValueFrom(
        this.http.post<ApiResponse<TokensDto>>(
          `${this.baseUrl}/auth/refresh`,
          { refreshToken: current.refreshToken },
          { context: new HttpContext().set(SKIP_AUTH, true) },
        ),
      );
      if (!response.success || !response.data) throw new Error('Refresh refused');
      this.save(response.data, current.remember);
      return true;
    } catch (error) {
      // Keep the session when the server is just unreachable; end it when the server refused it.
      const status = (error as { status?: number }).status;
      if (status === 0) return false;
      this.clear();
      this.expired.update((n) => n + 1);
      return false;
    }
  }

  private read(): StoredTokens | null {
    const raw = localStore.getItem(STORAGE_KEY) ?? sessionStore.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      const tokens = JSON.parse(raw) as StoredTokens;
      return tokens?.accessToken && tokens.refreshToken ? tokens : null;
    } catch {
      return null;
    }
  }
}
