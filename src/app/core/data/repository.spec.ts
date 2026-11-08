import { localStore } from './browser-storage';
import { EntityStore, LocalStorageRepository } from './repository';

interface Item {
  id: number;
  name: string;
}

const KEY = 'test-items';
const seed = (): Item[] => [{ id: 1, name: 'seeded' }];

describe('LocalStorageRepository and EntityStore', () => {
  beforeEach(() => localStore.clear());

  it('seeds on first run and persists to storage', async () => {
    const store = new EntityStore(new LocalStorageRepository<Item>(KEY, seed));
    await store.ready;
    expect(store.items().map((i) => i.name)).toEqual(['seeded']);
    expect(localStore.getItem(KEY)).toContain('seeded');
  });

  it('keeps changes across a reload', async () => {
    const first = new EntityStore(new LocalStorageRepository<Item>(KEY, seed));
    await first.ready;
    await first.save({ id: 1, name: 'edited' });
    await first.save({ id: 2, name: 'added' });

    const second = new EntityStore(new LocalStorageRepository<Item>(KEY, seed));
    await second.ready;
    expect(second.items().map((i) => i.name)).toEqual(['edited', 'added']);

    await second.remove(1);
    const third = new EntityStore(new LocalStorageRepository<Item>(KEY, seed));
    await third.ready;
    expect(third.items().map((i) => i.name)).toEqual(['added']);
  });

  it('restores defaults on reset', async () => {
    const store = new EntityStore(new LocalStorageRepository<Item>(KEY, seed));
    await store.ready;
    await store.remove(1);
    await store.reset();
    expect(store.items().map((i) => i.name)).toEqual(['seeded']);
  });

  it('recovers from corrupt data by restoring defaults', async () => {
    localStore.setItem(KEY, '{not json');
    vi.spyOn(console, 'warn').mockImplementation(() => undefined);

    const store = new EntityStore(new LocalStorageRepository<Item>(KEY, seed));
    await store.ready;
    expect(store.recovered()).toBe(true);
    expect(store.items().map((i) => i.name)).toEqual(['seeded']);
  });

  it('replaces data saved in an older shape with the defaults', async () => {
    localStore.setItem(KEY, JSON.stringify({ version: 1, data: [{ id: 1, name: 'old shape' }] }));
    vi.spyOn(console, 'warn').mockImplementation(() => undefined);

    const store = new EntityStore(new LocalStorageRepository<Item>(KEY, seed));
    await store.ready;
    expect(store.recovered()).toBe(true);
    expect(store.items().map((i) => i.name)).toEqual(['seeded']);
  });

  it('leaves the cached data unchanged when a write fails', async () => {
    const store = new EntityStore(new LocalStorageRepository<Item>(KEY, seed));
    await store.ready;
    const spy = vi.spyOn(localStore, 'setItem').mockImplementation(() => {
      throw new DOMException('full', 'QuotaExceededError');
    });

    await expect(store.save({ id: 2, name: 'too big' })).rejects.toThrow();
    expect(store.items().length).toBe(1);
    spy.mockRestore();
  });
});
