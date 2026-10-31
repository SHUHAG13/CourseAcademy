import { Injectable, computed, signal } from '@angular/core';

export interface DemoUser {
  name: string;
  email: string;
}

const STORAGE_KEY = 'ca-demo-user';

/**
 * UI-only authentication. No backend is connected: the "session" is kept in
 * localStorage so the navbar can show a signed-in state during demos.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly user = signal<DemoUser | null>(this.restore());
  readonly isLoggedIn = computed(() => this.user() !== null);

  async login(email: string): Promise<DemoUser> {
    await new Promise((r) => setTimeout(r, 600));
    const name = email.split('@')[0].replace(/[._-]+/g, ' ');
    return this.setUser({ name: titleCase(name), email });
  }

  async register(name: string, email: string): Promise<DemoUser> {
    await new Promise((r) => setTimeout(r, 700));
    return this.setUser({ name, email });
  }

  logout(): void {
    this.user.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  private setUser(user: DemoUser): DemoUser {
    this.user.set(user);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
    return user;
  }

  private restore(): DemoUser | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as DemoUser) : null;
    } catch {
      return null;
    }
  }
}

function titleCase(value: string): string {
  return value.replace(/\b\w/g, (c) => c.toUpperCase());
}
