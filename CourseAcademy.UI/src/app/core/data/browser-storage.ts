export type KeyValueStore = Pick<Storage, 'getItem' | 'setItem' | 'removeItem' | 'clear'>;

class MemoryStore implements KeyValueStore {
  private readonly values = new Map<string, string>();

  getItem(key: string): string | null {
    return this.values.get(key) ?? null;
  }

  setItem(key: string, value: string): void {
    this.values.set(key, value);
  }

  removeItem(key: string): void {
    this.values.delete(key);
  }

  clear(): void {
    this.values.clear();
  }
}

/** Browsers can block storage entirely. The site then still works, but nothing is saved. */
function resolve(name: 'localStorage' | 'sessionStorage'): KeyValueStore {
  try {
    return globalThis[name] ?? new MemoryStore();
  } catch {
    return new MemoryStore();
  }
}

export const localStore = resolve('localStorage');
export const sessionStore = resolve('sessionStorage');
