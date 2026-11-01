import { Injectable, computed, inject, signal } from '@angular/core';
import { localStore, sessionStore } from '../data/browser-storage';
import { USER_REPOSITORY } from '../data/repositories';
import { EntityStore, uid } from '../data/repository';
import { AppUser } from '../models/user.model';

const SESSION_KEY = 'ca-demo-session';

/**
 * Demo authentication. Accounts and the session live in browser storage, so
 * anyone using this browser could read or change them. It controls what the
 * interface shows and provides no real security. A real backend must verify
 * credentials and roles on the server.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly store = new EntityStore(inject(USER_REPOSITORY));
  private readonly sessionUserId = signal<string | null>(this.restoreSession());

  readonly ready = this.store.ready;
  readonly recovered = this.store.recovered;
  readonly users = this.store.items;
  readonly user = computed(() => this.users().find((u) => u.id === this.sessionUserId()) ?? null);
  readonly isLoggedIn = computed(() => this.user() !== null);
  readonly isAdmin = computed(() => this.user()?.role === 'admin');
  /** Where this user's account pages live. */
  readonly homeUrl = computed(() => (this.isAdmin() ? '/admin' : '/dashboard'));

  constructor() {
    if (typeof window !== 'undefined') {
      window.addEventListener('storage', (e) => {
        if (e.key === SESSION_KEY || e.key === null) this.sessionUserId.set(this.restoreSession());
      });
    }
  }

  /** Rejects with a message suitable for showing on the login form. */
  async login(email: string, password: string, remember = true): Promise<AppUser> {
    await new Promise((r) => setTimeout(r, 600));
    const user = this.findByEmail(email);
    if (!user || user.password !== password) {
      throw new Error('Incorrect email or password.');
    }
    this.startSession(user.id, remember);
    return user;
  }

  /** Public sign-up always creates the User role. */
  async register(name: string, email: string, password: string, phone?: string): Promise<AppUser> {
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
      role: 'user',
      createdAt: new Date().toISOString(),
    });
    this.startSession(user.id, true);
    return user;
  }

  logout(): void {
    this.sessionUserId.set(null);
    localStore.removeItem(SESSION_KEY);
    sessionStore.removeItem(SESSION_KEY);
  }

  reset(): Promise<void> {
    return this.store.reset();
  }

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
