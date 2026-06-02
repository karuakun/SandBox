import { Dropbox } from 'dropbox';
import type { Scenario } from '@macrodroid-manager/converter';
import type { DeployProvider, DeployResult, ExportEntry, DropboxProviderConfig } from './types/provider.js';

export class DropboxProvider implements DeployProvider {
  readonly name = 'dropbox';

  private readonly dbx: Dropbox;
  private readonly basePath: string;

  constructor(config: DropboxProviderConfig) {
    this.dbx = new Dropbox({ accessToken: config.accessToken });
    this.basePath = config.basePath.replace(/\/$/, '');
  }

  async testConnection(): Promise<boolean> {
    try {
      await this.dbx.usersGetCurrentAccount();
      return true;
    } catch {
      return false;
    }
  }

  async deployScenario(scenario: Scenario, mdrContent: Buffer): Promise<DeployResult> {
    const filename = `${scenario.metadata.id}_v${scenario.metadata.version}.mdr`;
    const path = `${this.basePath}/deploy/scenarios/${filename}`;
    try {
      await this.dbx.filesUpload({
        path,
        contents: mdrContent,
        mode: { '.tag': 'overwrite' },
        mute: false,
      });
      return { success: true, deployedAt: new Date().toISOString(), provider: this.name };
    } catch (err) {
      return { success: false, deployedAt: new Date().toISOString(), provider: this.name, error: String(err) };
    }
  }

  async deployScript(scriptName: string, content: string): Promise<DeployResult> {
    const path = `${this.basePath}/deploy/scripts/${scriptName}`;
    try {
      await this.dbx.filesUpload({
        path,
        contents: Buffer.from(content, 'utf-8'),
        mode: { '.tag': 'overwrite' },
        mute: false,
      });
      return { success: true, deployedAt: new Date().toISOString(), provider: this.name };
    } catch (err) {
      return { success: false, deployedAt: new Date().toISOString(), provider: this.name, error: String(err) };
    }
  }

  async listExports(): Promise<ExportEntry[]> {
    const folderPath = `${this.basePath}/export`;
    try {
      const res = await this.dbx.filesListFolder({ path: folderPath });
      return res.result.entries
        .filter((e) => e['.tag'] === 'file' && e.name.endsWith('.mdr'))
        .map((e) => ({
          id: e.id ?? e.name,
          filename: e.name,
          uploadedAt: 'server_modified' in e ? (e.server_modified as string) : '',
          sizeBytes: 'size' in e ? (e.size as number) : 0,
        }));
    } catch {
      return [];
    }
  }

  async fetchExport(entry: ExportEntry): Promise<Buffer> {
    const path = `${this.basePath}/export/${entry.filename}`;
    const res = await this.dbx.filesDownload({ path });
    const fileBinary = (res.result as { fileBinary: ArrayBuffer }).fileBinary;
    return Buffer.from(fileBinary);
  }

  async deleteExport(entry: ExportEntry): Promise<void> {
    const path = `${this.basePath}/export/${entry.filename}`;
    await this.dbx.filesDeleteV2({ path });
  }
}
