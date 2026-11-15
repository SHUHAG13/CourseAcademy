import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { ApiError } from '../api/api-error';
import { API_MODE } from '../api/api-mode';
import { ApiService } from '../api/api.service';
import { AuthSession } from '../api/auth-session';
import { AuthResultDto, MeDto, UserListItemDto } from '../api/auth.models';
import { PagedResult } from '../api/api.models';
import { localStore, sessionStore } from '../data/browser-storage';
import { USER_REPOSITORY } from '../data/repositories';
import { EntityStore, uid } from '../data/repository';
import { SCREENS, screenPermissions } from '../data/screens.data';
import { ADMIN_ROLE_ID, CourseGrant, STUDENT_ROLE_ID } from '../models/permission.model';
import { AppUser } from '../models/user.model';
import { ToastService } from './toast.service';

const SESSION_KEY = 'ca-demo-session';
const USERS_PAGE_SIZE = 100;

/**
 * Sign-in and the list of accounts.
 * - API mode: the ASP.NET Core API checks credentials and issues tokens; the user list is loaded
 *   only for people who may see it.
 * - Demo mode: accounts and the session live in browser storage and provide no real security.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiMode = inject(API_MODE);
  private readonly api = inject(ApiService);
  private readonly session = inject(AuthSession);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  // Demo mode
  private readonly store = new EntityStore(inject(USER_REPOSITORY));
  private readonly sessionUserId = signal<string | null>(this.apiMode ? null : this.restoreSession());

  // API mode
  private readonly me = signal<MeDto | null>(null);
  private readonly apiUsers = signal<UserListItemDto[]>([]);

  readonly ready: Promise<void> = this.apiMode ? this.restoreApiSession() : this.store.ready;
  readonly recovered = this.apiMode ? signal(false).asReadonly() : this.store.recovered;

  /** The signed-in user as the API describes them (API mode only). */
  readonly currentSession = this.me.asReadonly();

  readonly users = computed<AppUser[]>(() => (this.apiMode ? this.apiUsers().map(fromListItem) : this.store.items()));
  readonly user = computed<AppUser | null>(() => {
    if (this.apiMode) {
      const me = this.me();
      return me ? fromMe(me) : null;
    }
    return this.users().find((u) => u.id === this.sessionUserId()) ?? null;
  });
  readonly isLoggedIn = computed(() => this.user() !== null);
  readonly isAdmin = computed(() => this.user()?.role === ADMIN_ROLE_ID);

  /** Course grants of every listed user (API mode). */
  readonly listedGrants = computed<CourseGrant[]>(() =>
    this.apiUsers().flatMap((u) => u.courseGrants.map((g) => toGrant(String(u.id), g.courseId, g.permissions))),
  );

  constructor() {
    if (this.apiMode) {
      // The refresh token was refused: the user must sign in again.
      effect(() => {
        if (this.session.expired() === 0) return;
        untracked(() => this.endSession('Your session has expired. Please log in again.'));
      });
      // Another tab signed in or out.
      effect(() => {
        if (this.session.changedElsewhere() === 0) return;
        untracked(() => void this.restoreApiSession());
      });
    } else if (typeof window !== 'undefined') {
      window.addEventListener('storage', (e) => {
        if (e.key === SESSION_KEY || e.key === null) this.sessionUserId.set(this.restoreSession());
      });
    }
  }

  /** Rejects with a message suitable for showing on the login form. */
  async login(email: string, password: string, remember = true): Promise<AppUser> {
    if (this.apiMode) {
      const result = await this.api.post<AuthResultDto>('auth/login', { email: email.trim(), password, remember });
      return this.startApiSession(result, remember);
    }
    await new Promise((r) => setTimeout(r, 600));
    const user = this.findByEmail(email);
    if (!user || user.password !== password) {
      throw new Error('Incorrect email or password.');
    }
    this.startSession(user.id, remember);
    return user;
  }

  /** Public sign-up always creates the Student role. The form only submits once the terms are accepted. */
  async register(name: string, email: string, password: string, phone?: string): Promise<AppUser> {
    if (this.apiMode) {
      const result = await this.api.post<AuthResultDto>('auth/register', {
        name: name.trim(),
        email: email.trim(),
        password,
        phone: phone || null,
        acceptTerms: true,
      });
      return this.startApiSession(result, true);
    }
    await new Promise((r) => setTimeout(r, 700));
    if (this.findByEmail(email)) {
      throw new Error('An account with this email already exists.');
    }
    const user = await this.store.save({
      id: uid('user'),
      name,
      email: email.trim().toLowerCase(),
      phone: phone || undefined,
      password,
      role: STUDENT_ROLE_ID,
      createdAt: new Date().toISOString(),
    });
    this.startSession(user.id, true);
    return user;
  }

  /** Demo mode only: saves account changes. `PermissionService` checks permissions first. */
  updateUser(user: AppUser): Promise<AppUser> {
    return this.store.save(user);
  }

  logout(): void {
    if (this.apiMode) {
      const refreshToken = this.session.refreshToken;
      if (refreshToken) {
        // Best effort: the session ends locally even if the server can't be reached.
        this.api.post('auth/logout', { refreshToken }).catch(() => undefined);
      }
      this.clearApiSession();
      return;
    }
    this.sessionUserId.set(null);
    localStore.removeItem(SESSION_KEY);
    sessionStore.removeItem(SESSION_KEY);
  }

  /** API mode: reloads the signed-in user, e.g. after their access may have changed. */
  async reloadMe(): Promise<void> {
    if (!this.apiMode || !this.session.hasSession) return;
    this.me.set(await this.api.get<MeDto>('auth/me'));
  }

  /** API mode: reloads every page of the user list when the signed-in user may see it. */
  async loadUsers(): Promise<void> {
    if (!this.apiMode) return;
    const me = this.me();
    if (!me || !['users.view', 'access.view'].some((p) => me.permissions.includes(p as never))) {
      this.apiUsers.set([]);
      return;
    }
    const all: UserListItemDto[] = [];
    for (let page = 1; ; page++) {
      const result = await this.api.get<PagedResult<UserListItemDto>>('users', { page, pageSize: USERS_PAGE_SIZE });
      all.push(...result.items);
      if (all.length >= result.total || result.items.length === 0) break;
    }
    this.apiUsers.set(all);
  }

  reset(): Promise<void> {
    // Accounts in API mode live on the server; demo reset does not touch them.
    return this.apiMode ? Promise.resolve() : this.store.reset();
  }

  // ── API mode ────────────────────────────────────────────────

  private async restoreApiSession(): Promise<void> {
    if (!this.session.hasSession) {
      this.clearApiSession();
      return;
    }
    try {
      await this.reloadMe();
      await this.loadUsers();
    } catch (error) {
      // A refused session was already cleared by the interceptor. Keep tokens when the server is just unreachable.
      this.me.set(null);
      if (error instanceof ApiError && error.status === 401) this.session.clear();
    }
  }

  private async startApiSession(result: AuthResultDto, remember: boolean): Promise<AppUser> {
    this.session.save(result, remember);
    this.me.set(result.me);
    try {
      await this.loadUsers();
    } catch {
      // Signed in either way; admin lists load again when their page opens.
    }
    return fromMe(result.me);
  }

  private clearApiSession(): void {
    this.session.clear();
    this.me.set(null);
    this.apiUsers.set([]);
  }

  private endSession(message: string): void {
    const wasSignedIn = this.me() !== null;
    this.clearApiSession();
    if (!wasSignedIn) return;
    this.toast.error(message);
    const returnUrl = this.router.url;
    void this.router.navigate(['/login'], { queryParams: returnUrl && returnUrl !== '/login' ? { returnUrl } : {} });
  }

  // ── Demo mode ───────────────────────────────────────────────

  private findByEmail(email: string): AppUser | undefined {
    const normalised = email.trim().toLowerCase();
    return this.users().find((u) => u.email.toLowerCase() === normalised);
  }

  private startSession(userId: string, remember: boolean): void {
    localStore.removeItem(SESSION_KEY);
    sessionStore.removeItem(SESSION_KEY);
    (remember ? localStore : sessionStore).setItem(SESSION_KEY, userId);
    this.sessionUserId.set(userId);
  }

  private restoreSession(): string | null {
    return localStore.getItem(SESSION_KEY) ?? sessionStore.getItem(SESSION_KEY);
  }
}

/** The API's numeric ids become strings, so pages and models keep working unchanged. */
function fromMe(me: MeDto): AppUser {
  return {
    id: String(me.id),
    name: me.name,
    email: me.email,
    phone: me.phone ?? undefined,
    password: '',
    role: me.roleCode,
    createdAt: me.createdAt,
  };
}

function fromListItem(u: UserListItemDto): AppUser {
  return {
    id: String(u.id),
    name: u.name,
    email: u.email,
    phone: u.phone ?? undefined,
    password: '',
    role: u.roleCode,
    // A custom screen list becomes the permissions of those screens, as the access matrix expects.
    access: u.customScreens ? SCREENS.filter((s) => u.customScreens!.includes(s.id)).flatMap(screenPermissions) : undefined,
    createdAt: u.createdAt,
  };
}

export function toGrant(userId: string, courseId: number, permissions: CourseGrant['permissions']): CourseGrant {
  return { id: `grant-${userId}-${courseId}`, userId, courseId, permissions };
}
