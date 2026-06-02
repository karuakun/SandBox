import { readdir } from 'node:fs/promises';
import { join } from 'node:path';
import type { PlatformConverter } from './types/converter.js';

export class ConverterRegistry {
  private readonly converters = new Map<string, PlatformConverter>();

  register(converter: PlatformConverter): void {
    this.converters.set(converter.platformId, converter);
  }

  get(platformId: string): PlatformConverter | undefined {
    return this.converters.get(platformId);
  }

  list(): PlatformConverter[] {
    return [...this.converters.values()];
  }

  async loadFromDirectory(dir: string): Promise<{ loaded: string[]; errors: string[] }> {
    const loaded: string[] = [];
    const errors: string[] = [];

    let entries;
    try {
      entries = await readdir(dir, { withFileTypes: true });
    } catch {
      return { loaded, errors };
    }

    for (const entry of entries) {
      if (!entry.isDirectory()) continue;
      const indexPath = join(dir, entry.name, 'index.js');
      try {
        const mod = await import(indexPath) as { default: new () => PlatformConverter };
        const instance = new mod.default();
        this.register(instance);
        loaded.push(instance.platformId);
      } catch (err) {
        errors.push(`${entry.name}: ${String(err)}`);
      }
    }

    return { loaded, errors };
  }
}
