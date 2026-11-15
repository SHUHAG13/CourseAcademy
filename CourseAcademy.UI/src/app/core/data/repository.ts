import { signal } from '@angular/core';
import { localStore } from './browser-storage';

export interface Entity {
  id: string | number;
}

/**
 * Persistence contract used by every service. It is asynchronous so an HTTP
 * implementation backed by the ASP.NET Core API can replace the local one
 * without changing the services or pages that use it.
 */
export abstract class Repository<T extends Entity> {
  abstract list(): Promise<T[]>;
  abstract save(item: T): Promise<T>;
  abstract remove(id: T['id']): Promise<void>;
  /** Restores the default demo records. */
  abstract reset(): Promise<T[]>;
  /** Calls `listener` when the data changes elsewhere, e.g. in another tab. */
  abstract watch(listener: () => void): void;
}

interface Stored<T> {
  version: number;
  data: T[];
}

/** Bump when a stored record's shape changes. Older data is then replaced by the defaults. */
const SCHEMA_VERSION = 7;

/** Browser-storage implementation. This is a demo store, not a secure one. */
export class LocalStorageRepository<T extends Entity> extends Repository<T> {
  constructor(
    private readonly key: string,
    private readonly seed: () => T[],
  ) {
    super();
  }

  async list(): Promise<T[]> {
    return this.read();
  }

  async save(item: T): Promise<T> {
    const items = this.read();
    const index = items.findIndex((i) => i.id === item.id);
    if (index === -1) {
      items.push(item);
    } else {
      items[index] = item;
    }
    this.write(items);
    return item;
  }

  async remove(id: T['id']): Promise<void> {
    this.write(this.read().filter((i) => i.id !== id));
  }

  async reset(): Promise<T[]> {
    const items = this.seed();
    this.write(items);
    return items;
  }

  watch(listener: () => void): void {
    if (typeof window === 'undefined') return;
    window.addEventListener('storage', (e) => {
      if (e.key === this.key || e.key === null) listener();
    });
  }

  private read(): T[] {
    const raw = localStore.getItem(this.key);
    if (raw === null) {
      const items = this.seed();
      this.write(items);
      return items;
    }
    const stored = JSON.parse(raw) as Stored<T>;
    if (stored?.version !== SCHEMA_VERSION || !Array.isArray(stored.data)) {
      throw new Error(`Unreadable demo data in "${this.key}"`);
    }
    return stored.data;
  }

  private write(items: T[]): void {
    const stored: Stored<T> = { version: SCHEMA_VERSION, data: items };
    localStore.setItem(this.key, JSON.stringify(stored));
  }
}

/** Signal-backed cache of one repository, shared by the services. */
export class EntityStore<T extends Entity> {
  private readonly state = signal<T[]>([]);

  readonly items = this.state.asReadonly();
  /** True when saved data could not be read and the defaults were restored. */
  readonly recovered = signal(false);
  readonly ready: Promise<void>;

  constructor(private readonly repository: Repository<T>) {
    this.ready = this.reload();
    repository.watch(() => void this.reload());
  }

  async reload(): Promise<void> {
    try {
      this.state.set(await this.repository.list());
    } catch (error) {
      console.warn(error);
      this.recovered.set(true);
      // Start-up must not fail even if the defaults can't be saved, e.g. when storage is full.
      await this.reset().catch((resetError) => console.warn(resetError));
    }
  }

  async save(item: T): Promise<T> {
    await this.repository.save(item);
    this.state.update((items) =>
      items.some((i) => i.id === item.id) ? items.map((i) => (i.id === item.id ? item : i)) : [...items, item],
    );
    return item;
  }

  async remove(id: T['id']): Promise<void> {
    await this.repository.remove(id);
    this.state.update((items) => items.filter((i) => i.id !== id));
  }

  async reset(): Promise<void> {
    this.state.set(await this.repository.reset());
  }
}

/** Short unique id for records created in the browser. */
export function uid(prefix: string): string {
  return `${prefix}-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;
}
